using Dayspot.Api.Auth;
using Dayspot.Api.Dados;

namespace Dayspot.Api;

public sealed record CredenciaisJson(string? Email, string? Senha);

public sealed record EscolhaEmpresaJson(string? Slug);

public sealed record EmpresaJson(string Nome, string Slug, string Papel);

/// <summary>As empresas ativas do usuário; com mais de uma, a SPA mostra a escolha (2.10).</summary>
public sealed record LoginJson(IEnumerable<EmpresaJson> Empresas);

/// <summary>
/// Login e escolha da empresa da sessão (2.3; requisitos §9 e §12). Rodam na transação
/// por requisição; usuarios, memberships, tenants e sessoes ficam fora do RLS (guia
/// §009), então o tenant dela não importa.
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

    public static async Task<IResult> Entrar(HttpContext ctx, AppDb db, CancellationToken ct)
    {
        var entrada = await Json.Ler<CredenciaisJson>(ctx.Request, ct);
        if (entrada is null)
        {
            return Json.CorpoInvalido();
        }
        var email = Email.Normalizar(entrada.Email ?? "");
        if (email == "")
        {
            return Erros.CampoInvalido("email", "Informe o e-mail.");
        }
        var senha = entrada.Senha ?? ""; // sem Trim: espaço nas pontas faz parte da senha
        if (senha == "")
        {
            return Erros.CampoInvalido("senha", "Informe a senha.");
        }

        var usuario = await db.BuscarUsuarioPorEmail(email, ct);
        var certa = Senha.Conferir(senha, usuario?.SenhaHash ?? HashFalso) && usuario is not null;
        var empresas = certa ? await db.ListarEmpresasDoUsuario(usuario!.Id, ct) : [];
        if (empresas.Count == 0)
        {
            // Sem membership ativo também não entra, com a mesma resposta: não diz que a conta existe.
            return Erros.Responder(StatusCodes.Status401Unauthorized, Erros.CredencialInvalida, "E-mail ou senha incorretos.");
        }

        // Com uma empresa, a sessão já nasce nela; com várias, nula até a escolha.
        var tenantId = empresas.Count == 1 ? empresas[0].TenantId : (Guid?)null;
        var userAgent = ctx.Request.Headers.UserAgent.ToString();
        var sessao = await db.CriarSessao(usuario!.Id, tenantId, Duracao,
            userAgent == "" ? null : userAgent[..Math.Min(userAgent.Length, MaxUserAgent)], ct);
        ctx.Response.Cookies.Append(Cookie, sessao.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = Duracao,
        });
        return Json.Responder(StatusCodes.Status200OK,
            new LoginJson(empresas.Select(e => new EmpresaJson(e.Nome, e.Slug, e.Papel))));
    }

    /// <summary>
    /// Grava na sessão a empresa escolhida pelo slug. Única rota que recebe a empresa no
    /// corpo, e só para gravá-la na sessão (RNF05).
    /// </summary>
    public static async Task<IResult> EscolherEmpresa(HttpContext ctx, AppDb db, CancellationToken ct)
    {
        // Lê a sessão do cookie enquanto não há o middleware de autenticação (2.5).
        var sessao = Guid.TryParse(ctx.Request.Cookies[Cookie], out var id) ? await db.BuscarSessaoValida(id, ct) : null;
        if (sessao is null)
        {
            return Erros.Responder(StatusCodes.Status401Unauthorized, Erros.NaoAutenticado, "Sessão expirada. Entre de novo.");
        }
        var entrada = await Json.Ler<EscolhaEmpresaJson>(ctx.Request, ct);
        if (entrada is null)
        {
            return Json.CorpoInvalido();
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
