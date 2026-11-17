using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Dayspot.Api.Dados;

namespace Dayspot.Api.Tests;

public class TransacaoTests
{
    // Com uma conexão só no pool, toda requisição reusa a mesma, que é onde um SET
    // sem escopo local vazaria o tenant.
    private static string PoolDeUmaConexao() =>
        new NpgsqlConnectionStringBuilder(Banco.ConnectionString("DATABASE_URL")) { MaxPoolSize = 1 }.ConnectionString;

    [Fact]
    public async Task MesmaConexaoTenantsDiferentes()
    {
        var pool = PoolDeUmaConexao();
        var pid = 0;
        List<Guid> tenants = [];
        await using var api = await Servidor.ComTransacao(pool, app => app.MapGet("/", async (AppDb db) =>
        {
            pid = await db.Database.SqlQuery<int>($"SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
            // Sem WHERE: quem filtra é só o RLS, pelo tenant da transação.
            tenants = await db.Database.SqlQuery<Guid>($"SELECT DISTINCT tenant_id AS \"Value\" FROM clientes").ToListAsync();
        }));

        Assert.Equal(HttpStatusCode.OK, (await api.Chamar("GET", "/", tenant: Seed.Alfa)).StatusCode);
        var pidAlfa = pid;
        Assert.Equal([Seed.Alfa], tenants);

        Assert.Equal(HttpStatusCode.OK, (await api.Chamar("GET", "/", tenant: Seed.Beta)).StatusCode);
        Assert.True(pid == pidAlfa, $"conexões diferentes ({pidAlfa} e {pid}): o teste não provaria nada");
        Assert.Equal([Seed.Beta], tenants); // na conexão que foi do Alfa

        await using var conn = new NpgsqlConnection(pool);
        await conn.OpenAsync(TestContext.Current.CancellationToken);
        Assert.Equal(pidAlfa, await conn.Escalar<int>("SELECT pg_backend_pid()"));
        // Nada do tenant fica na conexão depois da requisição.
        Assert.Null(await conn.Escalar<string>("SELECT app.current_tenant_id()::text"));
    }

    public static TheoryData<string, int, bool> Respostas => new()
    {
        { "201", StatusCodes.Status201Created, true },
        { "400", StatusCodes.Status400BadRequest, false },
        { "500", StatusCodes.Status500InternalServerError, false },
        { "excecao", StatusCodes.Status500InternalServerError, false },
    };

    [Theory]
    [MemberData(nameof(Respostas))]
    public async Task CommitERollback(string caso, int status, bool grava)
    {
        var ct = TestContext.Current.CancellationToken;
        var pool = PoolDeUmaConexao();
        var id = Guid.Empty;
        await using var api = await Servidor.ComTransacao(pool, app => app.MapGet("/", async (HttpContext ctx, AppDb db) =>
        {
            id = (await db.CriarCliente(Seed.Alfa, "Teste 1.3", null)).Id;
            return caso switch
            {
                "201" => Json.Responder(StatusCodes.Status201Created, null),
                "400" => Erros.CampoInvalido("nome", "inválido"),
                "500" => Erros.Interno(ctx, new InvalidOperationException("teste")),
                _ => throw new InvalidOperationException("teste"),
            };
        }));

        var r = await api.Chamar("GET", "/", tenant: Seed.Alfa);
        Assert.Equal(status, (int)r.StatusCode);
        if (status >= 400)
        {
            await r.ExigirErro(r.StatusCode, status == 400 ? Erros.RequisicaoInvalida : Erros.ErroInterno);
        }

        await using var db = new AppDb(AppDb.Opcoes(pool));
        await using var tx = await db.IniciarComTenant(Seed.Alfa, ct);
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(grava, await db.BuscarCliente(Seed.Alfa, id, ct) is not null);
        if (grava)
        {
            await db.Database.ExecuteSqlAsync($"DELETE FROM clientes WHERE id = {id}", ct);
            await tx.CommitAsync(ct);
        }
    }

    // COMMIT que falha (transação abortada por SQL inválido) vira 500, mesmo com o
    // handler tendo respondido 201, e sem os cabeçalhos que ele setou.
    [Fact]
    public async Task FalhaNoCommitResponde500()
    {
        await using var api = await Servidor.ComTransacao(PoolDeUmaConexao(), app => app.MapGet("/", async (HttpContext ctx, AppDb db) =>
        {
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("SELECT * FROM tabela_que_nao_existe"));
            ctx.Response.Headers.Location = "/api/clientes/x";
            return Json.Responder(StatusCodes.Status201Created, null);
        }));
        var r = await api.Chamar("GET", "/", tenant: Seed.Alfa);
        await r.ExigirErro(HttpStatusCode.InternalServerError, Erros.ErroInterno);
        Assert.Null(r.Headers.Location);
    }

    // Banco fora do ar: o BEGIN falha e a resposta é 500 no formato padrão.
    [Fact]
    public async Task SemBancoResponde500()
    {
        var chamou = false;
        await using var api = await Servidor.ComTransacao(Banco.ForaDoAr, app => app.MapGet("/", () => { chamou = true; }));
        await (await api.Chamar("GET", "/", tenant: Seed.Alfa)).ExigirErro(HttpStatusCode.InternalServerError, Erros.ErroInterno);
        Assert.False(chamou, "handler chamado sem transação");
    }

    // Rota inexistente e método não permitido (1.13): formato da API e sem
    // transação. O banco está fora do ar, então abrir transação daria 500, como dá
    // na rota registrada.
    [Theory]
    [InlineData("GET", "/api/nao-existe", HttpStatusCode.NotFound, Erros.NaoEncontrado)]
    [InlineData("DELETE", "/api/clientes", HttpStatusCode.MethodNotAllowed, Erros.MetodoNaoPermitido)]
    [InlineData("GET", "/api/clientes", HttpStatusCode.InternalServerError, Erros.ErroInterno)] // abriu transação
    public async Task RotaInexistenteSemTransacao(string metodo, string rota, HttpStatusCode status, string codigo)
    {
        await using var api = await Servidor.ComTransacao(Banco.ForaDoAr, app => app.MapApi());
        var r = await api.Chamar(metodo, rota, tenant: Seed.Alfa);
        await r.ExigirErro(status, codigo);
        Assert.Equal("application/json; charset=utf-8", r.Content.Headers.ContentType?.ToString());
        if (status == HttpStatusCode.MethodNotAllowed)
        {
            Assert.Equal(["GET", "HEAD", "POST"], r.Content.Headers.Allow.Order());
        }
    }

    // A API inteira, com transação de verdade: o que o POST gravou aparece no GET
    // seguinte (COMMIT), e o HEAD responde como o GET (o corpo, quem corta é o Kestrel).
    [Fact]
    public async Task ApiCompletaGravaELe()
    {
        var ct = TestContext.Current.CancellationToken;
        var pool = PoolDeUmaConexao();
        await using var api = await Servidor.ComTransacao(pool, app => app.MapApi());

        var r = await api.Chamar("POST", "/api/clientes", """{"nome":"Teste API completa"}""", Seed.Alfa);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var id = (await r.Json()).GetProperty("id").GetGuid();
        try
        {
            r = await api.Chamar("GET", "/api/clientes", tenant: Seed.Alfa);
            Assert.Contains(id, (await r.Json()).EnumerateArray().Select(c => c.GetProperty("id").GetGuid()));
            // O Beta não enxerga o cliente do Alfa.
            r = await api.Chamar("GET", "/api/clientes", tenant: Seed.Beta);
            Assert.DoesNotContain(id, (await r.Json()).EnumerateArray().Select(c => c.GetProperty("id").GetGuid()));

            r = await api.Chamar("HEAD", "/api/clientes", tenant: Seed.Alfa);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }
        finally
        {
            await using var db = new AppDb(AppDb.Opcoes(pool));
            await using var tx = await db.IniciarComTenant(Seed.Alfa, ct);
            await db.Database.ExecuteSqlAsync($"DELETE FROM clientes WHERE id = {id}", ct);
            await tx.CommitAsync(ct);
        }
    }
}
