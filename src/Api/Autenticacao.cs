using Dayspot.Api.Dados;

namespace Dayspot.Api;

/// <summary>
/// Sessão do cookie antes do handler (2.5; requisitos §9). Sem sessão válida, 401
/// nao_autenticado; com ela, o handler acha a <see cref="Sessao"/> em
/// ctx.Features.Get&lt;Sessao&gt;().
/// </summary>
public static class Autenticacao
{
    // Sem sessão: cadastro, login e as rotas públicas (#162). O /api/saude responde no
    // UseRouting (ShortCircuit) e nem chega aqui.
    private static readonly PathString[] Abertas = ["/api/signup", "/api/login", "/api/publico"];

    /// <summary>Vem depois do UseRouting e antes do UseTransacaoNasRotas.</summary>
    public static IApplicationBuilder UseAutenticacao(this IApplicationBuilder app) =>
        app.UseWhen(ExigeSessao, ramo => ramo.Use(ConferirSessao));

    // Rota inexistente e 405 seguem para o 404/405 do UseTransacaoNasRotas.
    private static bool ExigeSessao(HttpContext ctx) =>
        ctx.GetEndpoint() is RouteEndpoint && !Abertas.Any(p => ctx.Request.Path.StartsWithSegments(p));

    private static async Task ConferirSessao(HttpContext ctx, RequestDelegate proximo)
    {
        Sessao? sessao = null;
        if (Guid.TryParse(ctx.Request.Cookies[Login.Cookie], out var id))
        {
            try
            {
                sessao = await ctx.RequestServices.GetRequiredService<AppDb>().BuscarSessaoValida(id, ctx.RequestAborted);
            }
            catch (Exception erro) when (!ctx.RequestAborted.IsCancellationRequested)
            {
                await Erros.Interno(ctx, erro).ExecuteAsync(ctx); // ainda fora do middleware de transação
                return;
            }
        }
        if (sessao is null)
        {
            await Erros.Responder(StatusCodes.Status401Unauthorized, Erros.NaoAutenticado, "Sessão expirada. Entre de novo.")
                .ExecuteAsync(ctx);
            return;
        }
        ctx.Features.Set(sessao);
        await proximo(ctx);
    }
}
