using Microsoft.EntityFrameworkCore;
using Dayspot.Api.Dados;

namespace Dayspot.Api;

public static class Transacao
{
    /// <summary>
    /// Abre uma transação por requisição (1.3), com o tenant de tenant(ctx):
    /// TENANT_FIXO na Fatia 1, a sessão na Fatia 2. Resposta 4xx/5xx ou exceção =
    /// ROLLBACK; o resto, COMMIT. Os handlers usam o <see cref="AppDb"/> da
    /// requisição, que já está nessa transação.
    ///
    /// A resposta do handler só sai depois do COMMIT: se ele falhar, o cliente recebe
    /// 500, não um 201 de algo que não foi gravado.
    ///
    /// Rota inexistente (404) e método não permitido (405) respondem no formato da
    /// API sem abrir transação (1.13). Vem depois do UseRouting, que é quem diz se a
    /// requisição casou com uma rota.
    /// </summary>
    public static IApplicationBuilder UseTransacaoNasRotas(this IApplicationBuilder app, Func<HttpContext, Guid> tenant) =>
        app.Use(async (ctx, next) =>
        {
            // O 405 do roteamento é um Endpoint sintético, não um RouteEndpoint.
            if (ctx.GetEndpoint() is not RouteEndpoint)
            {
                await next(ctx);
                await ErroDoRoteamento(ctx);
                return;
            }

            // Status e corpo ficam guardados até o fim da transação.
            var saida = ctx.Response.Body;
            using var adiada = new MemoryStream();
            ctx.Response.Body = adiada;
            try
            {
                var db = ctx.RequestServices.GetRequiredService<AppDb>();
                await using var tx = await db.IniciarComTenant(tenant(ctx), ctx.RequestAborted);
                await next(ctx);
                if (ctx.Response.StatusCode < 400)
                {
                    // COMMIT em transação abortada vira ROLLBACK sem erro no Postgres:
                    // o SELECT antes faz a falha aparecer.
                    await db.Database.ExecuteSqlRawAsync("SELECT 1", ctx.RequestAborted);
                    await tx.CommitAsync(ctx.RequestAborted);
                }
            }
            catch (Exception erro) when (!ctx.RequestAborted.IsCancellationRequested)
            {
                ctx.Response.Clear(); // o 500 não leva Location, Set-Cookie etc. do handler
                await Erros.Interno(ctx, erro).ExecuteAsync(ctx);
            }
            finally
            {
                ctx.Response.Body = saida;
            }
            ctx.Response.ContentLength = adiada.Length;
            adiada.Position = 0;
            await adiada.CopyToAsync(saida, ctx.RequestAborted);
        });

    // Troca o 404/405 sem corpo do roteamento pelo formato da API; o Allow do 405 fica.
    private static Task ErroDoRoteamento(HttpContext ctx)
    {
        if (ctx.Response.HasStarted)
        {
            return Task.CompletedTask;
        }
        return ctx.Response.StatusCode switch
        {
            StatusCodes.Status405MethodNotAllowed =>
                Erros.Responder(StatusCodes.Status405MethodNotAllowed, Erros.MetodoNaoPermitido, "Método não permitido nesta rota.").ExecuteAsync(ctx),
            StatusCodes.Status404NotFound =>
                Erros.Responder(StatusCodes.Status404NotFound, Erros.NaoEncontrado, "Rota não encontrada.").ExecuteAsync(ctx),
            _ => Task.CompletedTask,
        };
    }
}
