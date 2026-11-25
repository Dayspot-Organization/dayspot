using Dayspot.Api.Auth;
using Dayspot.Api.Dados;
using Microsoft.AspNetCore.Mvc;

namespace Dayspot.Api;

public sealed record CredenciaisJson(string? Email, string? Senha);

public sealed record EscolhaEmpresaJson(string? Slug);

public sealed record EmpresaJson(string Nome, string Slug, string Papel);

/// <summary>As empresas ativas do usuário; com mais de uma, a SPA mostra a escolha (2.10).</summary>
public sealed record LoginJson(IEnumerable<EmpresaJson> Empresas);

public sealed record UsuarioJson(Guid Id, string Nome, string Email);

/// <summary>Resposta do GET /api/me. EmpresaAtiva nula: sessão ainda sem empresa escolhida.</summary>
public sealed record EuJson(UsuarioJson Usuario, IEnumerable<EmpresaJson> Empresas, EmpresaJson? EmpresaAtiva);

/// <summary>
/// Login e escolha da empresa da sessão (2.3; requisitos §9 e §12). usuarios,
/// memberships, tenants e sessoes ficam fora do RLS (guia §009), então o tenant não
/// importa. O POST /api/login fica fora da transação por requisição
/// (Transacao.TransacaoPropria): cada consulta é um autocommit e a conexão volta ao pool
/// antes do Argon2.
/// </summary>
public static class Login
{
    /// <summary>Cookie com o id da sessão (HttpOnly, Secure, SameSite=Lax).</summary>
    public const string Cookie = "sessao";

    // ponytail: duração fixa no código; vai para o config se alguém precisar mudar.
    public static readonly TimeSpan Duracao = TimeSpan.FromDays(7);

    private const int MaxUserAgent = 512;

    // Conferido quando o e-mail não existe: a resposta leva o tempo de uma senha errada
    // e não revela, pelo tempo, se o e-mail tem conta.
    private static readonly string HashFalso = Senha.GerarHash(Guid.NewGuid().ToString());

    public static async Task<IResult> Entrar(HttpContext ctx, AppDb db, [FromServices] LimiteLogin limite, CancellationToken ct)
    {
        var (entrada, erroCorpo) = await Json.Ler<CredenciaisJson>(ctx.Request, ct);
        if (entrada is null)
        {
            return erroCorpo;
        }
        var email = Email.Normalizar(entrada.Email ?? "");
        if (email == "")
        {
            return Erros.CampoInvalido("email", "Informe o e-mail.");
        }
        // O e-mail vira chave do contador do limite: sem teto, cada IP guardaria chaves de até 1 MiB.
        if (email.Length > Cadastro.MaxEmail)
        {
            return Erros.CampoInvalido("email", "Informe um e-mail válido (até 254 caracteres).");
        }
        var senha = entrada.Senha ?? ""; // sem Trim: espaço nas pontas faz parte da senha
        if (senha == "")
        {
            return Erros.CampoInvalido("senha", "Informe a senha.");
        }

        // Acima do limite (2.8), recusa sem consultar o banco nem conferir a senha.
        var ip = IpReal.Chave(ctx);
        if (!limite.Tentar(email, ip, out var espera))
        {
            return Limites.Recusar(ctx, "login", espera, "Muitas tentativas de login. Aguarde e tente de novo.");
        }

        var usuario = await db.BuscarUsuarioPorEmail(email, ct);
        var certa = Senha.Conferir(senha, usuario?.SenhaHash ?? HashFalso) && usuario is not null;
        var empresas = certa ? await db.ListarEmpresasDoUsuario(usuario!.Id, ct) : [];
        if (empresas.Count == 0)
        {
            // Sem membership ativo também não entra, com a mesma resposta: não diz que a conta existe.
            return Erros.Responder(StatusCodes.Status401Unauthorized, Erros.CredencialInvalida, "E-mail ou senha incorretos.");
        }

        limite.Acertou(email, ip);
        // Com uma empresa, a sessão já nasce nela; com várias, nula até a escolha.
        await AbrirSessao(ctx, db, usuario!.Id, empresas.Count == 1 ? empresas[0].TenantId : null, ct);
        return Json.Responder(StatusCodes.Status200OK, new LoginJson(empresas.Select(ParaJson)));
    }

    /// <summary>Cria a sessão na empresa dada (nula: ainda sem escolha) e grava o cookie. Login e cadastro (#112).</summary>
    public static async Task AbrirSessao(HttpContext ctx, AppDb db, Guid usuarioId, Guid? tenantId, CancellationToken ct)
    {
        var userAgent = ctx.Request.Headers.UserAgent.ToString();
        var sessao = await db.CriarSessao(usuarioId, tenantId, Duracao,
            userAgent == "" ? null : userAgent[..Math.Min(userAgent.Length, MaxUserAgent)], ct);
        var opcoes = OpcoesDoCookie();
        opcoes.MaxAge = Duracao;
        ctx.Response.Cookies.Append(Cookie, sessao.ToString(), opcoes);
    }

    /// <summary>POST /api/logout (2.4): revoga a sessão do cookie e apaga o cookie.</summary>
    public static async Task<IResult> Sair(HttpContext ctx, AppDb db, CancellationToken ct)
    {
        var sessao = ctx.Features.Get<Sessao>()!; // o UseAutenticacao (2.5) já deu 401 sem ela
        await db.RevogarSessao(sessao.Id, ct);
        ctx.Response.Cookies.Delete(Cookie, OpcoesDoCookie()); // mesmos atributos do login, senão o navegador não apaga
        return Results.NoContent();
    }

    private static CookieOptions OpcoesDoCookie() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
    };

    /// <summary>
    /// GET /api/me (2.9): o usuário da sessão, as empresas com membership ativo e o papel
    /// em cada uma, e a empresa ativa da sessão. Nula com a empresa ainda não escolhida
    /// (várias memberships) ou com a membership dela removida depois do login.
    /// </summary>
    public static async Task<IResult> Eu(HttpContext ctx, AppDb db, CancellationToken ct)
    {
        var sessao = ctx.Features.Get<Sessao>()!; // o UseAutenticacao (2.5) já deu 401 sem ela
        var usuario = await db.BuscarUsuario(sessao.UsuarioId, ct);
        var empresas = await db.ListarEmpresasDoUsuario(sessao.UsuarioId, ct);
        var ativa = empresas.FirstOrDefault(e => e.TenantId == sessao.TenantId);
        return Json.Responder(StatusCodes.Status200OK, new EuJson(
            new UsuarioJson(usuario.Id, usuario.Nome, usuario.Email),
            empresas.Select(ParaJson),
            ativa is null ? null : ParaJson(ativa)));
    }

    private static EmpresaJson ParaJson(EmpresaDoUsuario e) => new(e.Nome, e.Slug, e.Papel);

    /// <summary>
    /// Grava na sessão a empresa escolhida pelo slug. Única rota que recebe a empresa no
    /// corpo, e só para gravá-la na sessão (RNF05).
    /// </summary>
    public static async Task<IResult> EscolherEmpresa(HttpContext ctx, AppDb db, CancellationToken ct)
    {
        var sessao = ctx.Features.Get<Sessao>()!; // o UseAutenticacao (2.5) já deu 401 sem ela
        var (entrada, erroCorpo) = await Json.Ler<EscolhaEmpresaJson>(ctx.Request, ct);
        if (entrada is null)
        {
            return erroCorpo;
        }
        var slug = (entrada.Slug ?? "").Trim().ToLowerInvariant();
        if (slug == "")
        {
            return Erros.CampoInvalido("slug", "Informe a empresa.");
        }
        if (await db.GravarEmpresaDaSessao(sessao.Id, slug, ct) == 0)
        {
            return Erros.Responder(StatusCodes.Status404NotFound, Erros.NaoEncontrado, "Empresa não encontrada.");
        }
        return Results.NoContent();
    }
}
