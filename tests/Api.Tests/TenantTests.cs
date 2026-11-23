using System.Net;
using System.Text;
using Npgsql;

namespace Dayspot.Api.Tests;

/// <summary>
/// Middleware de tenant (2.6), no pipeline do Program.cs (<see cref="Servidor.ComPublico"/>),
/// com sessões de verdade gravadas pelo dono das tabelas e apagadas no fim.
/// </summary>
public class TenantTests
{
    private static Task<Servidor> Api() => Servidor.ComPublico(Banco.ConnectionString("DATABASE_URL"));

    private static async Task<Guid> Sessao(NpgsqlConnection dono, Guid usuario, Guid? tenant)
    {
        var id = Guid.NewGuid();
        await dono.Exec("INSERT INTO sessoes (id, usuario_id, tenant_id, expira_em) VALUES ($1, $2, $3, now() + interval '1 hour')",
            id, usuario, (object?)tenant ?? DBNull.Value);
        return id;
    }

    private static Task<HttpResponseMessage> Chamar(Servidor api, Guid sessao, string metodo, string rota, string? corpo = null)
    {
        var req = new HttpRequestMessage(new HttpMethod(metodo), rota);
        req.Headers.Add("Cookie", $"{Login.Cookie}={sessao}");
        if (corpo is not null)
        {
            req.Content = new StringContent(corpo, Encoding.UTF8, "application/json");
        }
        return api.Http.SendAsync(req, TestContext.Current.CancellationToken);
    }

    private static async Task<List<Guid>> Ids(HttpResponseMessage r)
    {
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"esperava 200, veio {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return [.. (await r.Json()).EnumerateArray().Select(e => e.GetProperty("id").GetGuid())];
    }

    // Seed: agendamentos até 6 dias antes e depois do dia em que rodou; 92 dias é o máximo da rota.
    private static string Periodo() =>
        $"de={DateTime.UtcNow.Date.AddDays(-46):yyyy-MM-ddTHH:mm:ssZ}&ate={DateTime.UtcNow.Date.AddDays(46):yyyy-MM-ddTHH:mm:ssZ}";

    [Fact]
    public async Task SessaoSemEmpresaSoAcessaMeEEscolha()
    {
        await using var dono = await Banco.Conectar("MIGRATIONS_DATABASE_URL");
        var sessao = await Sessao(dono, Seed.AdminAlfa, null);
        try
        {
            await using var api = await Api();
            foreach (var (metodo, rota, corpo) in new[]
            {
                ("GET", "/api/clientes", null),
                ("POST", "/api/clientes", """{"nome":"Não Grava"}"""),
                ("GET", "/api/agendamentos", null),
                ("POST", "/api/agendamentos", $$"""{"cliente_id":"{{Seed.ClienteAlfa}}","inicio":"2099-02-01T09:00:00Z"}"""),
            })
            {
                await (await Chamar(api, sessao, metodo, rota, corpo)).ExigirErro(HttpStatusCode.Forbidden, Erros.SemPermissao);
            }
            Assert.Equal(0, await dono.Escalar<int>("SELECT count(*) FROM clientes WHERE nome = 'Não Grava'"));

            Assert.Equal(HttpStatusCode.OK, (await Chamar(api, sessao, "GET", "/api/me")).StatusCode);
            // Rota pública com o cookie: continua no tenant do slug.
            Assert.Equal(HttpStatusCode.OK, (await Chamar(api, sessao, "GET", "/api/publico/alfa/servicos")).StatusCode);

            Assert.Equal(HttpStatusCode.NoContent, (await Chamar(api, sessao, "POST", "/api/sessao/empresa", """{"slug":"alfa"}""")).StatusCode);
            Assert.Contains(Seed.ClienteAlfa, await Ids(await Chamar(api, sessao, "GET", "/api/clientes")));
        }
        finally
        {
            await dono.Exec("DELETE FROM sessoes WHERE id = $1", sessao);
        }
    }

    [Fact]
    public async Task MembershipRemovidoPerdeAcessoNaRequisicaoSeguinte()
    {
        await using var e = await EmpresaDeTeste.Criar();
        var ativo = await Sessao(e.Dono, e.Prestador, e.Tenant);
        var removido = await Sessao(e.Dono, e.Removido, e.Tenant);
        await using var api = await Api();

        Assert.Empty(await Ids(await Chamar(api, ativo, "GET", "/api/clientes")));
        await (await Chamar(api, removido, "GET", "/api/clientes")).ExigirErro(HttpStatusCode.Forbidden, Erros.SemPermissao);

        await e.Dono.Exec("UPDATE memberships SET removido_em = now() WHERE usuario_id = $1 AND tenant_id = $2", e.Prestador, e.Tenant);
        await (await Chamar(api, ativo, "GET", "/api/clientes")).ExigirErro(HttpStatusCode.Forbidden, Erros.SemPermissao);
        // O /api/me segue respondendo, sem empresa ativa (2.9).
        Assert.Equal(HttpStatusCode.OK, (await Chamar(api, ativo, "GET", "/api/me")).StatusCode);
        // O DELETE do tenant no Dispose leva as sessões (ON DELETE CASCADE).
    }

    [Fact]
    public async Task TenantNoCorpoOuNaQueryNaoMudaOTenant()
    {
        await using var dono = await Banco.Conectar("MIGRATIONS_DATABASE_URL");
        var sessao = await Sessao(dono, Seed.AdminAlfa, Seed.Alfa);
        var nome = $"Cliente {Guid.NewGuid():N}";
        try
        {
            await using var api = await Api();
            var lista = await Ids(await Chamar(api, sessao, "GET", $"/api/clientes?tenant_id={Seed.Beta}"));
            Assert.Contains(Seed.ClienteAlfa, lista);
            Assert.DoesNotContain(Seed.ClienteBeta, lista);

            var agenda = await Ids(await Chamar(api, sessao, "GET", $"/api/agendamentos?{Periodo()}&tenant_id={Seed.Beta}"));
            Assert.Contains(Seed.AgendamentoAlfa, agenda);
            Assert.DoesNotContain(Seed.AgendamentoBeta, agenda);

            // No corpo, campo desconhecido: 400, nada gravado.
            await (await Chamar(api, sessao, "POST", "/api/clientes", $$"""{"nome":"{{nome}}","tenant_id":"{{Seed.Beta}}"}"""))
                .ExigirErro(HttpStatusCode.BadRequest, Erros.RequisicaoInvalida);
            Assert.Equal(0, await dono.Escalar<int>("SELECT count(*) FROM clientes WHERE nome = $1", nome));

            var r = await Chamar(api, sessao, "POST", $"/api/clientes?tenant_id={Seed.Beta}", $$"""{"nome":"{{nome}}"}""");
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);
            Assert.Equal(Seed.Alfa, await dono.Escalar<Guid>("SELECT tenant_id FROM clientes WHERE nome = $1", nome));
        }
        finally
        {
            await dono.Exec("DELETE FROM clientes WHERE nome = $1", nome);
            await dono.Exec("DELETE FROM sessoes WHERE id = $1", sessao);
        }
    }

    [Fact]
    public async Task TenantNaoEnxergaDadosDoOutro()
    {
        await using var dono = await Banco.Conectar("MIGRATIONS_DATABASE_URL");
        var beta = await Sessao(dono, Seed.AdminBeta, Seed.Beta);
        // Sessão apontando para uma empresa em que o usuário não tem membership.
        var intrusa = await Sessao(dono, Seed.AdminBeta, Seed.Alfa);
        try
        {
            await using var api = await Api();
            var lista = await Ids(await Chamar(api, beta, "GET", "/api/clientes"));
            Assert.Contains(Seed.ClienteBeta, lista);
            Assert.DoesNotContain(Seed.ClienteAlfa, lista);

            var agenda = await Ids(await Chamar(api, beta, "GET", $"/api/agendamentos?{Periodo()}"));
            Assert.Contains(Seed.AgendamentoBeta, agenda);
            Assert.DoesNotContain(Seed.AgendamentoAlfa, agenda);

            await (await Chamar(api, beta, "POST", "/api/agendamentos",
                    $$"""{"cliente_id":"{{Seed.ClienteAlfa}}","inicio":"2099-02-01T09:00:00Z"}"""))
                .ExigirErro(HttpStatusCode.NotFound, Erros.NaoEncontrado);

            await (await Chamar(api, intrusa, "GET", "/api/clientes")).ExigirErro(HttpStatusCode.Forbidden, Erros.SemPermissao);
        }
        finally
        {
            await dono.Exec("DELETE FROM sessoes WHERE id IN ($1, $2)", beta, intrusa);
        }
    }
}
