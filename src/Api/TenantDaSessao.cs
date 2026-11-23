using Dayspot.Api.Dados;

namespace Dayspot.Api;

/// <summary>
/// Middleware de tenant (2.6; requisitos §9, regra 2). O tenant sai só da sessão, nunca
/// do corpo nem da query string (RNF05), e o membership é conferido a cada requisição:
/// remover o membro corta o acesso na seguinte. Sessão sem empresa ou sem membership
/// ativo nela: 403 sem_permissao. Rotas sem sessão (públicas, login, cadastro) passam.
/// </summary>
public static class TenantDaSessao
{
    // Sessão ainda sem empresa (2.3) chega a estas; não tocam em tabela com RLS.
    private static readonly HashSet<string> SemEmpresa =
        ["/api/me", "/api/logout", "/api/sessao/empresa", "/api/convites/{token}/aceite"];

    /// <summary>
    /// O tenant da sessão; sem sessão ou sem empresa, Guid.Empty, que o RLS não casa
    /// com linha nenhuma. Os handlers só chegam aqui depois do <see cref="UseTenantDaSessao"/>.
    /// </summary>
    public static Guid Ler(HttpContext ctx) => ctx.Features.Get<Sessao>()?.TenantId ?? Guid.Empty;

    /// <summary>
    /// Vem depois do UseTransacaoNasRotas: a consulta ao membership roda na transação da
    /// requisição, e o 403 a desfaz.
    /// </summary>
    public static IApplicationBuilder UseTenantDaSessao(this IApplicationBuilder app) =>
        app.Use(async (ctx, next) =>
        {
            var sessao = ctx.Features.Get<Sessao>();
            if (sessao is null || SemEmpresa.Contains((ctx.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? ""))
            {
                await next(ctx);
                return;
            }
            if (sessao.TenantId is not Guid tenant
                || !await ctx.RequestServices.GetRequiredService<AppDb>().PrestadorAtivo(tenant, sessao.UsuarioId, ctx.RequestAborted))
            {
                await Erros.Responder(StatusCodes.Status403Forbidden, Erros.SemPermissao, "Escolha uma empresa em que você tem acesso.")
                    .ExecuteAsync(ctx);
                return;
            }
            await next(ctx);
        });
}
