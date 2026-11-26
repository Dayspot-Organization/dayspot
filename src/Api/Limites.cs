using System.Globalization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dayspot.Api;

/// <summary>Limite de 5 cadastros por hora por IP (#112). Singleton: o contador é do processo.</summary>
public sealed class LimiteCadastro(TimeProvider relogio)
{
    private readonly LimitePorJanela _limite = new(5, TimeSpan.FromHours(1), relogio);

    public bool Tentar(string chave, out TimeSpan espera) => _limite.Tentar(chave, out espera);
}

/// <summary>
/// Limite do login (2.8): 5 tentativas falhas por e-mail e 20 por IP, em 15 minutos.
/// Singleton: o contador é do processo.
/// </summary>
public sealed class LimiteLogin(TimeProvider relogio)
{
    private static readonly TimeSpan Janela = TimeSpan.FromMinutes(15);
    private readonly LimitePorJanela _porEmail = new(5, Janela, relogio);
    private readonly LimitePorJanela _porIp = new(20, Janela, relogio);

    /// <summary>
    /// Reserva a tentativa antes de conferir a senha: contar só depois da falha deixaria
    /// requisições em paralelo passarem todas. false = acima de um dos limites.
    /// </summary>
    public bool Tentar(string email, string ip, out TimeSpan espera)
    {
        if (!_porIp.Tentar(ip, out espera))
        {
            return false;
        }
        if (_porEmail.Tentar(email, out espera))
        {
            return true;
        }
        _porIp.Devolver(ip); // recusada pelo e-mail: não conta para o IP
        return false;
    }

    /// <summary>Login certo: zera o e-mail e a tentativa não conta como falha do IP.</summary>
    public void Acertou(string email, string ip)
    {
        _porEmail.Zerar(email);
        _porIp.Devolver(ip);
    }
}

public static class Limites
{
    public static IServiceCollection AddLimites(this IServiceCollection servicos)
    {
        servicos.TryAddSingleton(TimeProvider.System);
        servicos.AddIpReal();
        servicos.AddSingleton<LimiteCadastro>();
        servicos.AddSingleton<LimiteLogin>();
        return servicos;
    }

    /// <summary>
    /// IP real só no POST /api/signup e no POST /api/login, depois do UseRouting e antes do
    /// UseTransacaoNasRotas. O limite em si é checado no handler.
    /// </summary>
    public static IApplicationBuilder UseLimites(this IApplicationBuilder app) =>
        app.UseWhen(EhRotaComLimite, ramo => ramo.UseForwardedHeaders());

    // Pelo endpoint, não pelo caminho: o roteamento também casa "/api/signup/".
    private static bool EhRotaComLimite(HttpContext ctx) =>
        HttpMethods.IsPost(ctx.Request.Method)
        && ctx.GetEndpoint() is RouteEndpoint { RoutePattern.RawText: "/api/signup" or "/api/login" };

    /// <summary>429 com Retry-After e um aviso no log, sem IP, e-mail nem corpo.</summary>
    public static IResult Recusar(HttpContext ctx, string limite, TimeSpan espera, string mensagem)
    {
        ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Limites))
            .LogWarning("429 pelo limite do {Limite}", limite);
        ctx.Response.Headers.RetryAfter = ((int)Math.Ceiling(espera.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        return Erros.Responder(StatusCodes.Status429TooManyRequests, Erros.MuitasTentativas, mensagem);
    }
}
