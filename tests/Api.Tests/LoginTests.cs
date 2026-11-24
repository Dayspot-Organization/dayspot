using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Dayspot.Api.Auth;
using Dayspot.Api.Dados;

namespace Dayspot.Api.Tests;

/// <summary>
/// POST /api/login e POST /api/sessao/empresa (2.3), GET /api/me (2.9) e POST /api/logout
/// (2.4). Usuários e empresas entram na transação do teste (<see cref="Servidor.ComBanco"/>,
/// com o UseAutenticacao), desfeita no fim.
/// </summary>
public class LoginTests
{
    private const string SenhaCerta = "senha-de-teste-123";

    private sealed record Empresa(Guid Id, string Slug);

    private static async Task<(Guid Id, string Email)> Usuario(AppDb db)
    {
        var id = Guid.NewGuid();
        var email = $"{id:N}@teste.example";
        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO usuarios (id, email, senha_hash, nome) VALUES ({id}, {email}, {Senha.GerarHash(SenhaCerta)}, 'Ana Teste')");
        return (id, email);
    }

    private static async Task<Empresa> NovaEmpresa(AppDb db, Guid criador, string nome)
    {
        var e = new Empresa(Guid.NewGuid(), $"t-{Guid.NewGuid():N}");
        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO tenants (id, nome, slug, criado_por) VALUES ({e.Id}, {nome}, {e.Slug}, {criador})");
        return e;
    }

    private static Task Membership(AppDb db, Guid usuario, Empresa e, string papel, bool removido = false) =>
        db.Database.ExecuteSqlAsync(
            $"INSERT INTO memberships (usuario_id, tenant_id, papel, removido_em) VALUES ({usuario}, {e.Id}, {papel}, {(removido ? DateTime.UtcNow : null)})");

    private static Task<HttpResponseMessage> Entrar(Servidor api, string email, string senha) =>
        api.Chamar("POST", "/api/login", $$"""{"email":"{{email}}","senha":"{{senha}}"}""");

    private static Task<HttpResponseMessage> Escolher(Servidor api, string? cookie, string slug)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/sessao/empresa")
        {
            Content = new StringContent($$"""{"slug":"{{slug}}"}""", Encoding.UTF8, "application/json"),
        };
        if (cookie is not null)
        {
            req.Headers.Add("Cookie", cookie);
        }
        return api.Http.SendAsync(req);
    }

    /// <summary>Confere o 200 e devolve o par nome=valor do cookie e o id da sessão.</summary>
    private static async Task<(string Cookie, Guid Sessao)> ExigirSessao(HttpResponseMessage r)
    {
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"esperava 200, veio {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        var setCookie = Assert.Single(r.Headers.GetValues("Set-Cookie"));
        var partes = setCookie.Split(';', StringSplitOptions.TrimEntries);
        Assert.StartsWith($"{Login.Cookie}=", partes[0]);
        Assert.Contains("httponly", partes);
        Assert.Contains("secure", partes);
        Assert.Contains("samesite=lax", partes);
        Assert.Contains("path=/", partes);
        Assert.Contains($"max-age={(int)Login.Duracao.TotalSeconds}", partes);
        return (partes[0], Guid.Parse(partes[0][(Login.Cookie.Length + 1)..]));
    }

    private static Task<HttpResponseMessage> Eu(Servidor api, string? cookie)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        if (cookie is not null)
        {
            req.Headers.Add("Cookie", cookie);
        }
        return api.Http.SendAsync(req);
    }

    private static Task<HttpResponseMessage> Sair(Servidor api, string? cookie)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/logout");
        if (cookie is not null)
        {
            req.Headers.Add("Cookie", cookie);
        }
        return api.Http.SendAsync(req);
    }

    /// <summary>GET /api/me com 200: (slug, papel) das empresas e o slug da ativa (nulo sem ela).</summary>
    private static async Task<(List<(string?, string?)> Empresas, string? Ativa)> LerEu(Servidor api, string cookie)
    {
        var r = await Eu(api, cookie);
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"esperava 200, veio {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        var corpo = await r.Json();
        var empresas = corpo.GetProperty("empresas").EnumerateArray()
            .Select(e => (e.GetProperty("slug").GetString(), e.GetProperty("papel").GetString())).ToList();
        var ativa = corpo.GetProperty("empresa_ativa");
        return (empresas, ativa.ValueKind == JsonValueKind.Null ? null : ativa.GetProperty("slug").GetString());
    }

    private static Task<Sessao> LerSessao(Servidor api, Guid id) => api.Db.Sessoes.SingleAsync(s => s.Id == id);

    [Fact]
    public async Task CredencialCertaCriaSessaoNaUnicaEmpresa()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa, autenticar: true);
        var (usuario, email) = await Usuario(api.Db);
        var empresa = await NovaEmpresa(api.Db, usuario, "Salão Teste");
        await Membership(api.Db, usuario, empresa, "administrador");

        // E-mail normalizado (2.15): maiúsculas e espaços nas pontas entram.
        var r = await Entrar(api, $"  {email.ToUpperInvariant()} ", SenhaCerta);
        var (_, id) = await ExigirSessao(r);

        var sessao = await LerSessao(api, id);
        Assert.Equal(usuario, sessao.UsuarioId);
        Assert.Equal(empresa.Id, sessao.TenantId);
        Assert.Null(sessao.RevogadoEm);
        Assert.True(sessao.ExpiraEm > sessao.CriadoEm);

        var empresas = (await r.Json()).GetProperty("empresas").EnumerateArray().ToList();
        var unica = Assert.Single(empresas);
        Assert.Equal("Salão Teste", unica.GetProperty("nome").GetString());
        Assert.Equal(empresa.Slug, unica.GetProperty("slug").GetString());
        Assert.Equal("administrador", unica.GetProperty("papel").GetString());
    }

    [Fact]
    public async Task SemMembershipAtivoNaoEntra()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa, autenticar: true);
        var (semNenhuma, email1) = await Usuario(api.Db);
        var (soRemovida, email2) = await Usuario(api.Db);
        var empresa = await NovaEmpresa(api.Db, soRemovida, "Empresa Antiga");
        await Membership(api.Db, soRemovida, empresa, "prestador", removido: true);

        foreach (var email in new[] { email1, email2 })
        {
            var r = await Entrar(api, email, SenhaCerta);
            await r.ExigirErro(HttpStatusCode.Unauthorized, Erros.CredencialInvalida);
            Assert.False(r.Headers.Contains("Set-Cookie"));
        }
        Assert.Equal(0, await api.Db.Sessoes.CountAsync(s => s.UsuarioId == semNenhuma || s.UsuarioId == soRemovida));
    }

    [Fact]
    public async Task VariasEmpresasSessaoSemTenantAteEscolher()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa, autenticar: true);
        var (usuario, email) = await Usuario(api.Db);
        var beta = await NovaEmpresa(api.Db, usuario, "B Empresa");
        var alfa = await NovaEmpresa(api.Db, usuario, "A Empresa");
        var removida = await NovaEmpresa(api.Db, usuario, "C Removida");
        await Membership(api.Db, usuario, beta, "prestador");
        await Membership(api.Db, usuario, alfa, "administrador");
        await Membership(api.Db, usuario, removida, "prestador", removido: true);

        var r = await Entrar(api, email, SenhaCerta);
        var (cookie, id) = await ExigirSessao(r);
        Assert.Null((await LerSessao(api, id)).TenantId);

        // Só as ativas, em ordem de nome, com o papel em cada uma.
        var empresas = (await r.Json()).GetProperty("empresas").EnumerateArray()
            .Select(e => (e.GetProperty("slug").GetString(), e.GetProperty("papel").GetString())).ToList();
        Assert.Equal([(alfa.Slug, "administrador"), (beta.Slug, "prestador")], empresas);

        var escolha = await Escolher(api, cookie, beta.Slug.ToUpperInvariant());
        Assert.Equal(HttpStatusCode.NoContent, escolha.StatusCode);
        Assert.Equal(beta.Id, (await LerSessao(api, id)).TenantId);
    }

    [Fact]
    public async Task SlugSemMembershipAtivoEh404ESessaoNaoMuda()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa, autenticar: true);
        var (usuario, email) = await Usuario(api.Db);
        var (outro, _) = await Usuario(api.Db);
        var a = await NovaEmpresa(api.Db, usuario, "A");
        var b = await NovaEmpresa(api.Db, usuario, "B");
        var removida = await NovaEmpresa(api.Db, usuario, "Removida");
        var alheia = await NovaEmpresa(api.Db, outro, "De outro");
        await Membership(api.Db, usuario, a, "prestador");
        await Membership(api.Db, usuario, b, "prestador");
        await Membership(api.Db, usuario, removida, "prestador", removido: true);
        await Membership(api.Db, outro, alheia, "administrador");

        var (cookie, id) = await ExigirSessao(await Entrar(api, email, SenhaCerta));
        foreach (var slug in new[] { removida.Slug, alheia.Slug, "nao-existe" })
        {
            await (await Escolher(api, cookie, slug)).ExigirErro(HttpStatusCode.NotFound, Erros.NaoEncontrado);
            Assert.Null((await LerSessao(api, id)).TenantId);
        }
    }

    [Fact]
    public async Task EmailInexistenteESenhaErradaTemAMesmaResposta()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa, autenticar: true);
        var (usuario, email) = await Usuario(api.Db);
        await Membership(api.Db, usuario, await NovaEmpresa(api.Db, usuario, "X"), "administrador");

        var semConta = await Entrar(api, $"{Guid.NewGuid():N}@teste.example", SenhaCerta);
        var senhaErrada = await Entrar(api, email, SenhaCerta + "x");
        foreach (var r in new[] { semConta, senhaErrada })
        {
            await r.ExigirErro(HttpStatusCode.Unauthorized, Erros.CredencialInvalida);
            Assert.False(r.Headers.Contains("Set-Cookie"));
        }
        Assert.Equal(await semConta.Content.ReadAsStringAsync(), await senhaErrada.Content.ReadAsStringAsync());
        Assert.Equal(0, await api.Db.Sessoes.CountAsync(s => s.UsuarioId == usuario));
    }

    [Fact]
    public async Task EscolherEmpresaSemSessaoValidaEh401()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa, autenticar: true);
        var (usuario, email) = await Usuario(api.Db);
        var a = await NovaEmpresa(api.Db, usuario, "A");
        await Membership(api.Db, usuario, a, "administrador");
        await Membership(api.Db, usuario, await NovaEmpresa(api.Db, usuario, "B"), "administrador");
        var (cookie, id) = await ExigirSessao(await Entrar(api, email, SenhaCerta));

        await (await Escolher(api, null, a.Slug)).ExigirErro(HttpStatusCode.Unauthorized, Erros.NaoAutenticado);
        await (await Escolher(api, $"{Login.Cookie}=lixo", a.Slug)).ExigirErro(HttpStatusCode.Unauthorized, Erros.NaoAutenticado);
        await (await Escolher(api, $"{Login.Cookie}={Guid.NewGuid()}", a.Slug)).ExigirErro(HttpStatusCode.Unauthorized, Erros.NaoAutenticado);

        await api.Db.Database.ExecuteSqlAsync($"UPDATE sessoes SET revogado_em = now() WHERE id = {id}");
        await (await Escolher(api, cookie, a.Slug)).ExigirErro(HttpStatusCode.Unauthorized, Erros.NaoAutenticado);

        // expira_em > criado_em pelo CHECK: o criado_em recua junto.
        await api.Db.Database.ExecuteSqlAsync(
            $"UPDATE sessoes SET revogado_em = NULL, criado_em = now() - interval '2 days', expira_em = now() - interval '1 day' WHERE id = {id}");
        await (await Escolher(api, cookie, a.Slug)).ExigirErro(HttpStatusCode.Unauthorized, Erros.NaoAutenticado);
        Assert.Null((await LerSessao(api, id)).TenantId);
    }

    [Fact]
    public async Task LogoutRevogaASessaoEApagaOCookie()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa, autenticar: true);
        var (usuario, email) = await Usuario(api.Db);
        await Membership(api.Db, usuario, await NovaEmpresa(api.Db, usuario, "A"), "administrador");
        var (cookie, id) = await ExigirSessao(await Entrar(api, email, SenhaCerta));

        var r = await Sair(api, cookie);
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        // Apagar exige os mesmos atributos do login; a data no passado faz o navegador descartar.
        var partes = Assert.Single(r.Headers.GetValues("Set-Cookie")).Split(';', StringSplitOptions.TrimEntries);
        Assert.Equal($"{Login.Cookie}=", partes[0]);
        Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", partes);
        Assert.Contains("httponly", partes);
        Assert.Contains("secure", partes);
        Assert.Contains("samesite=lax", partes);
        Assert.Contains("path=/", partes);
        Assert.NotNull((await LerSessao(api, id)).RevogadoEm);

        // O cookie antigo, reenviado, não vale mais.
        await (await Eu(api, cookie)).ExigirErro(HttpStatusCode.Unauthorized, Erros.NaoAutenticado);
        await (await Sair(api, cookie)).ExigirErro(HttpStatusCode.Unauthorized, Erros.NaoAutenticado);
    }

    [Fact]
    public async Task LogoutSemSessaoValidaEh401()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa, autenticar: true);
        foreach (var cookie in new[] { null, $"{Login.Cookie}=lixo", $"{Login.Cookie}={Guid.NewGuid()}" })
        {
            await (await Sair(api, cookie)).ExigirErro(HttpStatusCode.Unauthorized, Erros.NaoAutenticado);
        }
    }

    [Fact]
    public async Task EuSemSessaoValidaEh401()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa, autenticar: true);
        foreach (var cookie in new[] { null, $"{Login.Cookie}=lixo", $"{Login.Cookie}={Guid.NewGuid()}" })
        {
            await (await Eu(api, cookie)).ExigirErro(HttpStatusCode.Unauthorized, Erros.NaoAutenticado);
        }
    }

    [Fact]
    public async Task EuComUmaEmpresaTrazUsuarioPapelEEmpresaAtiva()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa, autenticar: true);
        var (usuario, email) = await Usuario(api.Db);
        var empresa = await NovaEmpresa(api.Db, usuario, "Salão Teste");
        await Membership(api.Db, usuario, empresa, "administrador");
        var (cookie, _) = await ExigirSessao(await Entrar(api, email, SenhaCerta));

        var corpo = await (await Eu(api, cookie)).Json();
        var u = corpo.GetProperty("usuario");
        Assert.Equal(usuario, u.GetProperty("id").GetGuid());
        Assert.Equal("Ana Teste", u.GetProperty("nome").GetString());
        Assert.Equal(email, u.GetProperty("email").GetString());
        Assert.False(u.TryGetProperty("senha_hash", out _));
        var ativa = corpo.GetProperty("empresa_ativa");
        Assert.Equal("Salão Teste", ativa.GetProperty("nome").GetString());
        Assert.Equal(empresa.Slug, ativa.GetProperty("slug").GetString());
        Assert.Equal("administrador", ativa.GetProperty("papel").GetString());
        Assert.Equal([(empresa.Slug, "administrador")], (await LerEu(api, cookie)).Empresas);
    }

    [Fact]
    public async Task EuComVariasEmpresasSemEscolhaTemAtivaNula()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa, autenticar: true);
        var (usuario, email) = await Usuario(api.Db);
        var beta = await NovaEmpresa(api.Db, usuario, "B Empresa");
        var alfa = await NovaEmpresa(api.Db, usuario, "A Empresa");
        await Membership(api.Db, usuario, beta, "prestador");
        await Membership(api.Db, usuario, alfa, "administrador");
        await Membership(api.Db, usuario, await NovaEmpresa(api.Db, usuario, "C Removida"), "prestador", removido: true);
        var (cookie, _) = await ExigirSessao(await Entrar(api, email, SenhaCerta));

        var (empresas, ativa) = await LerEu(api, cookie);
        Assert.Equal([(alfa.Slug, "administrador"), (beta.Slug, "prestador")], empresas);
        Assert.Null(ativa);

        Assert.Equal(HttpStatusCode.NoContent, (await Escolher(api, cookie, beta.Slug)).StatusCode);
        Assert.Equal(beta.Slug, (await LerEu(api, cookie)).Ativa);
    }

    [Fact]
    public async Task EuSemAMembershipDaEmpresaAtivaTemAtivaNula()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa, autenticar: true);
        var (usuario, email) = await Usuario(api.Db);
        var a = await NovaEmpresa(api.Db, usuario, "A");
        var b = await NovaEmpresa(api.Db, usuario, "B");
        await Membership(api.Db, usuario, a, "administrador");
        await Membership(api.Db, usuario, b, "prestador");
        var (cookie, _) = await ExigirSessao(await Entrar(api, email, SenhaCerta));
        Assert.Equal(HttpStatusCode.NoContent, (await Escolher(api, cookie, a.Slug)).StatusCode);

        // Removida depois da escolha: some da lista e deixa de ser a ativa.
        await api.Db.Database.ExecuteSqlAsync(
            $"UPDATE memberships SET removido_em = now() WHERE usuario_id = {usuario} AND tenant_id = {a.Id}");
        var (empresas, ativa) = await LerEu(api, cookie);
        Assert.Equal([(b.Slug, "prestador")], empresas);
        Assert.Null(ativa);
    }

    [Theory]
    [InlineData("{", "")] // JSON malformado
    [InlineData("""{"senha":"x"}""", "email")]
    [InlineData("""{"email":"  ","senha":"x"}""", "email")]
    [InlineData("""{"email":"a@b.example"}""", "senha")]
    [InlineData("""{"email":"a@b.example","senha":"x","tenant_id":"x"}""", "")] // campo desconhecido
    public async Task CorpoInvalidoEh400(string corpo, string campo)
    {
        await using var api = await Servidor.SemBanco();
        var r = await api.Chamar("POST", "/api/login", corpo);
        await r.ExigirErro(HttpStatusCode.BadRequest, Erros.RequisicaoInvalida);
        if (campo != "")
        {
            Assert.Equal(campo, (await r.Json()).GetProperty("erro").GetProperty("detalhes").GetProperty("campo").GetString());
        }
    }

    // A validação vem antes de qualquer consulta: a sessão do ComBanco basta.
    [Theory]
    [InlineData("{", "")] // JSON malformado
    [InlineData("""{"slug":"  "}""", "slug")]
    [InlineData("""{"slug":"alfa","tenant_id":"x"}""", "")] // campo desconhecido
    public async Task EscolherEmpresaCorpoInvalidoEh400(string corpo, string campo)
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa);
        var r = await api.Chamar("POST", "/api/sessao/empresa", corpo);
        await r.ExigirErro(HttpStatusCode.BadRequest, Erros.RequisicaoInvalida);
        if (campo != "")
        {
            Assert.Equal(campo, (await r.Json()).GetProperty("erro").GetProperty("detalhes").GetProperty("campo").GetString());
        }
    }
}

/// <summary>
/// Limite do login (2.8) com o banco fora do ar: tentativa que passa do limite chega ao
/// banco e dá 500; a recusada sai em 429 sem consultar nada (nem conferir a senha).
/// </summary>
public class LimiteLoginTests
{
    private static RelogioAjustavel Relogio() => new(new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero));

    private static Task<Servidor> Api(TimeProvider relogio, LogEmMemoria? log = null) =>
        Servidor.ComTransacao(Banco.ForaDoAr, app => app.MapApi(), relogio, log);

    private static Task<HttpResponseMessage> Entrar(Servidor api, string email, string ip, string? encaminhado = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/login")
        {
            Content = new StringContent($$"""{"email":"{{email}}","senha":"errada"}""", Encoding.UTF8, "application/json"),
        };
        req.Headers.Add("CF-Connecting-IP", ip);
        if (encaminhado is not null)
        {
            req.Headers.Add("X-Forwarded-For", encaminhado);
        }
        return api.Http.SendAsync(req);
    }

    private static void Passou(HttpResponseMessage r) =>
        Assert.Equal(HttpStatusCode.InternalServerError, r.StatusCode); // chegou ao banco

    private static async Task ExigirLimite(HttpResponseMessage r, int retryAfter)
    {
        await r.ExigirErro(HttpStatusCode.TooManyRequests, Erros.MuitasTentativas);
        Assert.Equal(retryAfter.ToString(), Assert.Single(r.Headers.GetValues("Retry-After")));
    }

    [Fact]
    public async Task SextaDoMesmoEmailEh429()
    {
        var relogio = Relogio();
        await using var api = await Api(relogio);
        for (var i = 0; i < 5; i++)
        {
            Passou(await Entrar(api, "ana@alfa.example", $"203.0.113.{i}"));
        }
        relogio.Agora += TimeSpan.FromMinutes(5);
        // E-mail normalizado: maiúsculas e espaços contam como o mesmo.
        await ExigirLimite(await Entrar(api, " ANA@alfa.example ", "203.0.113.9"), 600);
    }

    [Fact]
    public async Task VigesimaPrimeiraDoMesmoIpEh429()
    {
        await using var api = await Api(Relogio());
        for (var i = 0; i < 20; i++)
        {
            Passou(await Entrar(api, $"u{i}@alfa.example", "203.0.113.1"));
        }
        await ExigirLimite(await Entrar(api, "outro@alfa.example", "203.0.113.1"), 900);
        Passou(await Entrar(api, "outro@alfa.example", "203.0.113.2"));
    }

    [Fact]
    public async Task XForwardedForForjadoNaoMudaOIp()
    {
        await using var api = await Api(Relogio());
        for (var i = 0; i < 20; i++)
        {
            Passou(await Entrar(api, $"u{i}@alfa.example", "203.0.113.1", $"198.51.100.{i}"));
        }
        await ExigirLimite(await Entrar(api, "outro@alfa.example", "203.0.113.1", "198.51.100.200"), 900);
    }

    // Recusada pelo e-mail não gasta o limite do IP.
    [Fact]
    public async Task RecusaPorEmailNaoContaParaOIp()
    {
        await using var api = await Api(Relogio());
        for (var i = 0; i < 5; i++)
        {
            Passou(await Entrar(api, "ana@alfa.example", "203.0.113.1"));
        }
        for (var i = 0; i < 20; i++)
        {
            await ExigirLimite(await Entrar(api, "ana@alfa.example", "203.0.113.1"), 900);
        }
        for (var i = 0; i < 15; i++)
        {
            Passou(await Entrar(api, $"u{i}@alfa.example", "203.0.113.1"));
        }
        await ExigirLimite(await Entrar(api, "outro@alfa.example", "203.0.113.1"), 900);
    }

    [Fact]
    public async Task PassadaAJanelaVoltaAAceitar()
    {
        var relogio = Relogio();
        await using var api = await Api(relogio);
        for (var i = 0; i < 5; i++)
        {
            Passou(await Entrar(api, "ana@alfa.example", "203.0.113.1"));
        }
        await ExigirLimite(await Entrar(api, "ana@alfa.example", "203.0.113.1"), 900);
        relogio.Agora += TimeSpan.FromMinutes(15);
        Passou(await Entrar(api, "ana@alfa.example", "203.0.113.1"));
    }

    [Fact]
    public async Task LimiteGeraLogSemDadoPessoal()
    {
        var log = new LogEmMemoria();
        await using var api = await Api(Relogio(), log);
        for (var i = 0; i < 5; i++)
        {
            Passou(await Entrar(api, "ana@alfa.example", "203.0.113.1"));
        }
        await ExigirLimite(await Entrar(api, "ana@alfa.example", "203.0.113.1"), 900);
        Assert.Single(log.Linhas, l => l.StartsWith("429 pelo limite do login"));
        Assert.All(log.Linhas, l =>
        {
            Assert.DoesNotContain("203.0.113", l);
            Assert.DoesNotContain("ana@", l);
        });
    }

    // Com banco: a senha certa zera o contador do e-mail.
    [Fact]
    public async Task LoginCertoZeraOContadorDoEmail()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa, relogio: Relogio());
        var id = Guid.NewGuid();
        var email = $"{id:N}@teste.example";
        await api.Db.Database.ExecuteSqlAsync(
            $"INSERT INTO usuarios (id, email, senha_hash, nome) VALUES ({id}, {email}, {Senha.GerarHash("senha-certa-123")}, 'Ana Teste')");
        await api.Db.Database.ExecuteSqlAsync(
            $"INSERT INTO memberships (usuario_id, tenant_id, papel) VALUES ({id}, {Seed.Alfa}, 'prestador')");
        Task<HttpResponseMessage> Entrar(string senha) =>
            api.Chamar("POST", "/api/login", $$"""{"email":"{{email}}","senha":"{{senha}}"}""");

        for (var i = 0; i < 4; i++)
        {
            await (await Entrar("errada")).ExigirErro(HttpStatusCode.Unauthorized, Erros.CredencialInvalida);
        }
        Assert.Equal(HttpStatusCode.OK, (await Entrar("senha-certa-123")).StatusCode);
        for (var i = 0; i < 5; i++)
        {
            await (await Entrar("errada")).ExigirErro(HttpStatusCode.Unauthorized, Erros.CredencialInvalida);
        }
        await (await Entrar("senha-certa-123")).ExigirErro(HttpStatusCode.TooManyRequests, Erros.MuitasTentativas);
    }
}
