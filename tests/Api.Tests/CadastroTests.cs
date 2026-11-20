using System.Net;
using System.Text.Json;

namespace Dayspot.Api.Tests;

public class CadastroTests
{
    private const string Rota = "/api/signup";

    // Corpo válido: cada caso estraga um campo só.
    private static string Corpo(string campo, object? valor)
    {
        var d = new Dictionary<string, object?>
        {
            ["nome"] = "Ana Souza",
            ["email"] = "ana@alfa.example",
            ["senha"] = "12345678",
            ["empresa_nome"] = "Alfa Barbearia",
            ["empresa_slug"] = "alfa-barbearia",
            ["contato"] = "(11) 99999-0000",
        };
        d[campo] = valor;
        return JsonSerializer.Serialize(d.Where(p => p.Value is not null).ToDictionary(p => p.Key, p => p.Value));
    }

    public static TheoryData<string, string> Invalidos()
    {
        var d = new TheoryData<string, string>();
        void Add(string campo, object? valor) => d.Add(campo, Corpo(campo, valor));
        Add("nome", "");
        Add("nome", "   ");
        Add("nome", new string('a', 201));
        Add("email", "");
        Add("email", "semarroba");
        Add("email", "a@");
        Add("email", "@b");
        Add("email", new string('a', 250) + "@b.co"); // 255
        Add("senha", null);
        Add("senha", "1234567");
        Add("senha", "😀😀😀😀😀😀😀"); // 7 caracteres, 14 unidades UTF-16
        Add("empresa_nome", "");
        Add("empresa_nome", new string('a', 121));
        Add("empresa_slug", "");
        Add("empresa_slug", "-ab");
        Add("empresa_slug", "ab");
        Add("empresa_slug", "a b");
        Add("empresa_slug", "loja_1");
        Add("empresa_slug", "lojá");
        Add("empresa_slug", new string('a', 51));
        Add("contato", null);
        Add("contato", "   ");
        Add("contato", new string('a', 121));
        return d;
    }

    [Theory]
    [MemberData(nameof(Invalidos))]
    public async Task CampoInvalido(string campo, string json)
    {
        await using var api = await Servidor.SemBanco();
        var r = await api.Chamar("POST", Rota, json);
        var corpo = await r.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"esperava 400, veio {(int)r.StatusCode} {corpo}");
        var erro = JsonDocument.Parse(corpo).RootElement.GetProperty("erro");
        Assert.Equal(Erros.RequisicaoInvalida, erro.GetProperty("codigo").GetString());
        Assert.Equal(campo, erro.GetProperty("detalhes").GetProperty("campo").GetString());
    }

    [Theory]
    [InlineData("{")]
    [InlineData("""{"nome":"Ana Souza","email":"ana@alfa.example","senha":"12345678","empresa_nome":"Alfa","empresa_slug":"alfa-barbearia","contato":"x","fuso_horario":"UTC"}""")]
    public async Task CorpoMalformadoOuCampoDesconhecido(string json)
    {
        await using var api = await Servidor.SemBanco();
        await (await api.Chamar("POST", Rota, json)).ExigirErro(HttpStatusCode.BadRequest, Erros.RequisicaoInvalida);
    }

    // Com o banco fora do ar, chegar a ele é a prova de que a validação passou.
    [Theory]
    [InlineData("senha", "12345678")]
    [InlineData("contato", 120)]
    [InlineData("empresa_nome", 120)]
    [InlineData("empresa_slug", " Minha-Loja ")]
    [InlineData("empresa_slug", "abc")]
    [InlineData("empresa_slug", "slug50")]
    [InlineData("email", "email254")]
    [InlineData("senha", "1234567 ")] // sem Trim: o espaço conta
    [InlineData("senha", "çççççççç")] // 8 caracteres Unicode
    public async Task PassaDaValidacao(string campo, object valor)
    {
        var texto = valor is int n ? new string('x', n) : (string)valor;
        texto = texto switch
        {
            "slug50" => new string('a', 50),
            "email254" => new string('a', 249) + "@b.co",
            _ => texto,
        };
        await using var api = await Servidor.SemBanco();
        await Assert.ThrowsAnyAsync<Exception>(() => api.Chamar("POST", Rota, Corpo(campo, texto)));
    }
}
