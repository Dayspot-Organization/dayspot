using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Dayspot.Api.Dados;

namespace Dayspot.Api.Tests;

public class AutenticacaoTests
{
    /// <summary>
    /// MapApi atrás do UseAutenticacao, mais GET /api/login e GET /api/signup de mentira
    /// (os reais são POST) e /api/eu, que devolve o usuário que chegou ao handler.
    /// </summary>
    private static Task<Servidor> Api(string conexao) => Servidor.Criar(
        s => s.AddSingleton(AppDb.Opcoes(conexao)).AddScoped<AppDb>()
            .AddSingleton(new Fixos(Seed.Alfa, Seed.Prestador, Seed.ServicoAlfa)),
        app =>
        {
            app.UseRouting();
            app.UseAutenticacao();
            app.MapApi();
            app.MapGet("/api/login", () => "aberta");
            app.MapGet("/api/signup", () => "aberta");
            app.MapGet("/api/eu", (HttpContext ctx) => ctx.Features.Get<Sessao>()!.UsuarioId);
        });

    private static Task<HttpResponseMessage> Get(Servidor api, string rota, string? cookie)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, rota);
        if (cookie is not null)
        {
            req.Headers.Add("Cookie", $"{Login.Cookie}={cookie}");
        }
        return api.Http.SendAsync(req, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nao-e-uuid")]
    public async Task SemSessao401(string? cookie)
    {
        // Banco fora do ar: o 401 sai sem consultar nada.
        await using var api = await Api(Banco.ForaDoAr);
        await (await Get(api, "/api/clientes", cookie)).ExigirErro(HttpStatusCode.Unauthorized, Erros.NaoAutenticado);
    }

    [Theory]
    [InlineData("/api/saude")]
    [InlineData("/api/login")]
    [InlineData("/api/signup")]
    public async Task RotasAbertasNaoConsultamSessao(string rota)
    {
        // Com cookie e banco fora do ar: se passasse pelo middleware, seria 500.
        await using var api = await Api(Banco.ForaDoAr);
        Assert.Equal(HttpStatusCode.OK, (await Get(api, rota, Guid.NewGuid().ToString())).StatusCode);
    }

    [Fact]
    public async Task RotaInexistenteContinua404()
    {
        await using var api = await Api(Banco.ForaDoAr);
        Assert.Equal(HttpStatusCode.NotFound, (await Get(api, "/api/nada", null)).StatusCode);
    }

    [Fact]
    public async Task PublicoSemPassarPeloMiddleware()
    {
        await using var e = await EmpresaDeTeste.Criar();
        await using var api = await Servidor.ComPublico(Banco.ConnectionString("DATABASE_URL"));
        // Sem cookie e com um que não é sessão: as duas respondem como antes.
        Assert.Equal(HttpStatusCode.OK, (await Get(api, $"/api/publico/{e.Slug}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Get(api, $"/api/publico/{e.Slug}/servicos", Guid.NewGuid().ToString())).StatusCode);
    }

    [Fact]
    public async Task SessaoValidaChegaAoHandler()
    {
        await using var dono = await Banco.Conectar("MIGRATIONS_DATABASE_URL");
        var valida = Guid.NewGuid();
        var expirada = Guid.NewGuid();
        var revogada = Guid.NewGuid();
        await dono.Exec("""
            INSERT INTO sessoes (id, usuario_id, tenant_id, criado_em, expira_em, revogado_em) VALUES
              ($1, $4, $5, now(), now() + interval '1 hour', NULL),
              ($2, $4, $5, now() - interval '2 hours', now() - interval '1 second', NULL),
              ($3, $4, $5, now(), now() + interval '1 hour', now())
            """, valida, expirada, revogada, Seed.AdminAlfa, Seed.Alfa);
        try
        {
            await using var api = await Api(Banco.ConnectionString("DATABASE_URL"));

            var r = await Get(api, "/api/eu", valida.ToString());
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Equal(Seed.AdminAlfa, (await r.Json()).GetGuid());

            foreach (var invalida in new[] { expirada, revogada, Guid.NewGuid() })
            {
                await (await Get(api, "/api/eu", invalida.ToString())).ExigirErro(HttpStatusCode.Unauthorized, Erros.NaoAutenticado);
            }
        }
        finally
        {
            await dono.Exec("DELETE FROM sessoes WHERE id IN ($1, $2, $3)", valida, expirada, revogada);
        }
    }
}
