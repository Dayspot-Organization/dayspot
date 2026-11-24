using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dayspot.Api;

/// <summary>Leitura e escrita de JSON da API: campos em snake_case, datas em UTC.</summary>
public static class Json
{
    public const string ContentType = "application/json; charset=utf-8";

    private const int LimiteCorpo = 1 << 20; // 1 MiB

    public static readonly JsonSerializerOptions Opcoes = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        // Campo com nome errado vira 400, não some calado.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        // Acentos saem como estão, não como \uXXXX.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static IResult Responder(int status, object? valor) =>
        Results.Json(valor, Opcoes, ContentType, status);

    /// <summary>
    /// Decodifica o corpo. Valor nulo = corpo inválido (JSON malformado, campo desconhecido,
    /// lixo depois do objeto, corpo truncado ou mais de 1 MiB), e Erro é a resposta 400.
    /// </summary>
    public static async Task<(T? Valor, IResult Erro)> Ler<T>(HttpRequest req, CancellationToken ct) where T : class
    {
        using var corpo = new MemoryStream();
        var bloco = new byte[16 * 1024];
        try
        {
            int lidos;
            while ((lidos = await req.Body.ReadAsync(bloco, ct)) > 0)
            {
                if (corpo.Length + lidos > LimiteCorpo)
                {
                    return (null, Erros.Responder(StatusCodes.Status400BadRequest, Erros.RequisicaoInvalida,
                        "Corpo da requisição acima de 1 MiB."));
                }
                corpo.Write(bloco, 0, lidos);
            }
            return (JsonSerializer.Deserialize<T>(corpo.GetBuffer().AsSpan(0, (int)corpo.Length), Opcoes), CorpoInvalido);
        }
        // Corpo truncado ou chunked malformado: o Kestrel avisa na leitura.
        catch (Exception e) when (e is JsonException or BadHttpRequestException)
        {
            return (null, CorpoInvalido);
        }
    }

    // Imutável: a mesma resposta serve para toda requisição.
    private static readonly IResult CorpoInvalido =
        Erros.Responder(StatusCodes.Status400BadRequest, Erros.RequisicaoInvalida, "Corpo da requisição não é um JSON válido.");
}
