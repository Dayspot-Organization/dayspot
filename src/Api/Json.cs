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
    /// Decodifica o corpo. Nulo = corpo inválido (JSON malformado, campo desconhecido,
    /// lixo depois do objeto ou mais de 1 MiB): quem chama responde com
    /// <see cref="CorpoInvalido"/>.
    /// </summary>
    public static async Task<T?> Ler<T>(HttpRequest req, CancellationToken ct) where T : class
    {
        using var corpo = new MemoryStream();
        var bloco = new byte[16 * 1024];
        int lidos;
        while ((lidos = await req.Body.ReadAsync(bloco, ct)) > 0)
        {
            if (corpo.Length + lidos > LimiteCorpo)
            {
                return null;
            }
            corpo.Write(bloco, 0, lidos);
        }
        try
        {
            return JsonSerializer.Deserialize<T>(corpo.GetBuffer().AsSpan(0, (int)corpo.Length), Opcoes);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static IResult CorpoInvalido() =>
        Erros.Responder(StatusCodes.Status400BadRequest, Erros.RequisicaoInvalida, "Corpo da requisição não é um JSON válido.");
}
