using System.Net;
using System.Net.Http.Headers;

namespace Dayspot.Api;

public sealed record MensagemEmail(string Para, string Assunto, string Texto, string Html);

/// <summary>
/// Envio de e-mail transacional (#210). Devolve false quando o provedor falha ou demora;
/// a rota responde 503 com <see cref="Erros.EmailNaoEnviado"/>. O teste troca por um falso.
/// </summary>
public interface IEnvioEmail
{
    Task<bool> Enviar(MensagemEmail mensagem, CancellationToken ct);
}

/// <summary>API HTTP do Resend (POST /emails), sem SDK. Typed client do IHttpClientFactory.</summary>
public sealed class EnvioResend(HttpClient http, Config cfg, ILogger<EnvioResend> log) : IEnvioEmail
{
    public static readonly TimeSpan Prazo = TimeSpan.FromSeconds(10);

    private sealed record Corpo(string From, string[] To, string Subject, string Text, string Html);

    public async Task<bool> Enviar(MensagemEmail m, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(new Corpo(cfg.EmailRemetente, [m.Para], m.Assunto, m.Texto, m.Html),
                options: new(System.Text.Json.JsonSerializerDefaults.Web)),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ResendApiKey);
        try
        {
            using var r = await http.SendAsync(req, ct);
            if (r.IsSuccessStatusCode)
            {
                return true;
            }
            // Só o status: o corpo do Resend pode repetir o destinatário.
            log.LogError("Resend recusou o envio: {Status}", (int)r.StatusCode);
            return false;
        }
        catch (Exception e) when (e is HttpRequestException || (e is TaskCanceledException && !ct.IsCancellationRequested))
        {
            // O prazo do HttpClient estoura como TaskCanceledException sem o ct cancelado.
            log.LogError("Resend fora do alcance: {Erro}", e.GetType().Name);
            return false;
        }
    }
}

/// <summary>Development sem RESEND_API_KEY: o e-mail vai só para o log, para testar local.</summary>
public sealed class EnvioNoLog(ILogger<EnvioNoLog> log) : IEnvioEmail
{
    public Task<bool> Enviar(MensagemEmail m, CancellationToken ct)
    {
        log.LogInformation("e-mail só no log (sem RESEND_API_KEY) para {Para}: {Assunto}\n{Texto}", m.Para, m.Assunto, m.Texto);
        return Task.FromResult(true);
    }
}

/// <summary>Os modelos em pt-BR, com texto e HTML simples.</summary>
public static class ModelosEmail
{
    public static MensagemEmail CodigoVerificacao(string para, string codigo) => new(
        para,
        $"Seu código do Dayspot: {codigo}",
        $"""
        Seu código para confirmar o e-mail no Dayspot é {codigo}.

        Ele vale por 10 minutos. Se não foi você que pediu, ignore este e-mail.
        """,
        Html($"""
        <p>Seu código para confirmar o e-mail no Dayspot é:</p>
        <p style="font-size:28px;font-weight:600;letter-spacing:4px">{WebUtility.HtmlEncode(codigo)}</p>
        <p>Ele vale por 10 minutos. Se não foi você que pediu, ignore este e-mail.</p>
        """));

    public static MensagemEmail RedefinirSenha(string para, string link) => new(
        para,
        "Redefinir sua senha do Dayspot",
        $"""
        Para escolher uma senha nova no Dayspot, abra o link:

        {link}

        Ele vale por 30 minutos e funciona uma vez. Se não foi você que pediu, ignore este e-mail: sua senha continua a mesma.
        """,
        Html($"""
        <p>Para escolher uma senha nova no Dayspot, use o link abaixo.</p>
        <p><a href="{WebUtility.HtmlEncode(link)}">Redefinir senha</a></p>
        <p>Ele vale por 30 minutos e funciona uma vez. Se não foi você que pediu, ignore este e-mail: sua senha continua a mesma.</p>
        """));

    private static string Html(string corpo) =>
        $"""<!doctype html><html lang="pt-BR"><body style="font-family:system-ui,sans-serif;color:#18181B;font-size:15px;line-height:1.5">{corpo}</body></html>""";
}

public static class Emails
{
    /// <summary>Resend com chave; sem chave (o Config só aceita em Development), o log.</summary>
    public static IServiceCollection AddEmail(this IServiceCollection servicos, Config cfg)
    {
        if (cfg.ResendApiKey == "")
        {
            return servicos.AddSingleton<IEnvioEmail, EnvioNoLog>();
        }
        servicos.AddSingleton(cfg);
        servicos.AddHttpClient<IEnvioEmail, EnvioResend>(h =>
        {
            h.BaseAddress = new Uri("https://api.resend.com/");
            h.Timeout = EnvioResend.Prazo;
            h.DefaultRequestHeaders.UserAgent.ParseAdd("dayspot-api");
        });
        return servicos;
    }
}
