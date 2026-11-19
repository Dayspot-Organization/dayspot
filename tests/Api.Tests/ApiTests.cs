using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Dayspot.Api.Dados;

namespace Dayspot.Api.Tests;

public class ErrosTests
{
    private static DefaultHttpContext Contexto() => new()
    {
        RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        Response = { Body = new MemoryStream() },
    };

    private static async Task<string> Executar(IResult resultado, DefaultHttpContext ctx)
    {
        await resultado.ExecuteAsync(ctx);
        ctx.Response.Body.Position = 0;
        return await new StreamReader(ctx.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Responder()
    {
        var ctx = Contexto();
        var corpo = await Executar(Erros.Responder(StatusCodes.Status409Conflict, Erros.ConflitoHorario,
            "Horário ocupado.", new() { ["profissional_id"] = 7 }), ctx);

        Assert.Equal(StatusCodes.Status409Conflict, ctx.Response.StatusCode);
        Assert.StartsWith("application/json", ctx.Response.ContentType);
        var erro = JsonDocument.Parse(corpo).RootElement.GetProperty("erro");
        Assert.Equal(Erros.ConflitoHorario, erro.GetProperty("codigo").GetString());
        Assert.Equal("Horário ocupado.", erro.GetProperty("mensagem").GetString());
        Assert.Equal(7, erro.GetProperty("detalhes").GetProperty("profissional_id").GetInt32());
    }

    [Fact]
    public async Task InternoNaoVazaMensagem()
    {
        var ctx = Contexto();
        ctx.Request.Method = "POST";
        ctx.Request.Path = "/agendamentos";
        var corpo = await Executar(Erros.Interno(ctx,
            new InvalidOperationException("ERROR: relation \"agendamentos\" does not exist (SQLSTATE 42P01)")), ctx);

        Assert.Equal(StatusCodes.Status500InternalServerError, ctx.Response.StatusCode);
        Assert.DoesNotContain("SQLSTATE", corpo);
        Assert.DoesNotContain("relation", corpo);
        Assert.DoesNotContain("detalhes", corpo);
        Assert.Contains("\"codigo\":\"erro_interno\"", corpo);
    }
}

public class ClientesTests
{
    [Fact]
    public async Task CriarEListar()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa);

        var r = await api.Chamar("POST", "/api/clientes", """{"nome":"  Carla Teste ","telefone":"11 90000-0000"}""");
        var corpo = await r.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(r.StatusCode == HttpStatusCode.Created, $"criar: {r.StatusCode} {corpo}");
        Assert.Equal("application/json; charset=utf-8", r.Content.Headers.ContentType?.ToString());
        var criado = await r.Json();
        Assert.Equal("Carla Teste", criado.GetProperty("nome").GetString());
        Assert.Equal("11 90000-0000", criado.GetProperty("telefone").GetString());
        Assert.EndsWith("Z", criado.GetProperty("criado_em").GetString());
        // Só estes campos: vinculo_token é credencial e não pode sair na resposta.
        Assert.Equal(["id", "nome", "telefone", "criado_em"], criado.EnumerateObject().Select(p => p.Name));

        r = await api.Chamar("GET", "/api/clientes");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var lista = (await r.Json()).EnumerateArray().ToList();
        var ids = lista.Select(c => c.GetProperty("id").GetGuid()).ToList();
        Assert.Contains(criado.GetProperty("id").GetGuid(), ids);
        Assert.Contains(Seed.ClienteAlfa, ids);
        Assert.DoesNotContain(Seed.ClienteBeta, ids); // cliente do Beta na listagem do Alfa
        // Sem telefone sai null, não some; em ordem de nome.
        var doSeed = lista.Single(c => c.GetProperty("id").GetGuid() == Seed.ClienteAlfa);
        Assert.Equal(JsonValueKind.Null, doSeed.GetProperty("telefone").ValueKind);
        var nomes = lista.Select(c => c.GetProperty("nome").GetString()).ToList();
        Assert.Equal(["Alice Cliente Alfa", "Andre Cliente Alfa", "Artur Cliente Alfa"], nomes.Where(n => n!.EndsWith("Cliente Alfa")));
    }

    [Fact]
    public async Task TelefoneVazioViraNulo()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa);
        var r = await api.Chamar("POST", "/api/clientes", """{"nome":"Ana","telefone":"  "}""");
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await r.Json()).GetProperty("telefone").ValueKind);
    }

    public static TheoryData<string, string> Invalidos => new()
    {
        { "json malformado", """{"nome":""" },
        { "corpo vazio", "" },
        { "sem nome", """{"telefone":"1199"}""" },
        { "nome só espaços", """{"nome":"   "}""" },
        { "nome longo", $$"""{"nome":"{{new string('a', 201)}}"}""" },
        { "telefone longo", $$"""{"nome":"Ana","telefone":"{{new string('9', 31)}}"}""" },
        { "campo desconhecido", """{"nome":"Ana","email":"a@b.c"}""" },
        { "dois objetos", """{"nome":"Ana"}{"nome":"Bia"}""" },
        { "nome não é texto", """{"nome":7}""" },
    };

    [Theory]
    [MemberData(nameof(Invalidos))]
    public async Task CriarInvalido(string caso, string corpo)
    {
        _ = caso;
        await using var api = await Servidor.SemBanco();
        await (await api.Chamar("POST", "/api/clientes", corpo)).ExigirErro(HttpStatusCode.BadRequest, Erros.RequisicaoInvalida);
    }

    [Fact]
    public async Task CorpoAcimaDe1MiB()
    {
        await using var api = await Servidor.SemBanco();
        var corpo = $$"""{"nome":"Ana","telefone":"{{new string('9', 1 << 20)}}"}""";
        await (await api.Chamar("POST", "/api/clientes", corpo)).ExigirErro(HttpStatusCode.BadRequest, Erros.RequisicaoInvalida);
    }

    // 200 caracteres, não 200 unidades UTF-16: cada emoji conta um.
    [Fact]
    public async Task NomeNoLimiteContaCaracteres()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa);
        var nome = string.Concat(Enumerable.Repeat("😀", 200));
        var r = await api.Chamar("POST", "/api/clientes", JsonSerializer.Serialize(new { nome }));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.Equal(nome, (await r.Json()).GetProperty("nome").GetString());
    }
}

public class AgendamentosTests
{
    private static readonly DateTime Dia = new(2099, 1, 10, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Listar()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa);
        // Em 2099, longe das datas relativas do seed.
        (Guid Prestador, DateTime Inicio)[] agendamentos =
        [
            (Seed.Prestador, Dia.AddHours(10)),
            (Seed.AdminAlfa, Dia.AddHours(9)),
            (Seed.Prestador, Dia.AddDays(10)), // fora do dia
        ];
        foreach (var (prestador, inicio) in agendamentos)
        {
            await api.Db.Database.ExecuteSqlAsync($"""
                INSERT INTO agendamentos (tenant_id, cliente_id, servico_id, prestador_id, inicio, fim, valor_centavos)
                VALUES ({Seed.Alfa}, {Seed.ClienteAlfa}, {Seed.ServicoAlfa}, {prestador}, {inicio}, {inicio.AddHours(1)}, 0)
                """, TestContext.Current.CancellationToken);
        }
        async Task<List<JsonElement>> Listar(string query)
        {
            var r = await api.Chamar("GET", "/api/agendamentos?" + query);
            var corpo = await r.Content.ReadAsStringAsync();
            Assert.True(r.StatusCode == HttpStatusCode.OK, $"{query}: {r.StatusCode} {corpo}");
            Assert.DoesNotContain("-03:00", corpo); // datas fora de UTC
            return (await r.Json()).EnumerateArray().ToList();
        }

        // -03:00 de propósito: 2099-01-09T21:00-03:00 = 2099-01-10T00:00Z.
        var doDia = await Listar("de=2099-01-09T21:00:00-03:00&ate=2099-01-11T00:00:00Z");
        Assert.Equal(["2099-01-10T09:00:00Z", "2099-01-10T10:00:00Z"], doDia.Select(a => a.GetProperty("inicio").GetString()));
        Assert.Equal("Andre Cliente Alfa", doDia[0].GetProperty("cliente_nome").GetString());
        Assert.Equal(["id", "cliente_id", "cliente_nome", "servico_id", "prestador_id", "inicio", "fim", "status", "valor_centavos"],
            doDia[0].EnumerateObject().Select(p => p.Name));

        var doPrestador = await Listar($"de=2099-01-10T00:00:00Z&ate=2099-01-11T00:00:00Z&prestador_id={Seed.Prestador}");
        Assert.Equal(Seed.Prestador, Assert.Single(doPrestador).GetProperty("prestador_id").GetGuid());

        Assert.Equal(3, (await Listar("de=2099-01-01T00:00:00Z&ate=2099-02-01T00:00:00Z")).Count); // mês todo, sem prestador
        Assert.Empty(await Listar("de=2099-03-01T00:00:00Z")); // período vazio: [], não null
        Assert.Equal(2, (await Listar("ate=2099-01-11T00:00:00Z")).Count); // sem de: ate - 7 dias
    }

    // Sem de nem ate: de hoje a hoje + 7 dias. O seed sempre tem agendamento futuro nessa janela.
    [Fact]
    public async Task ListarSemPeriodo()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa);
        var r = await api.Chamar("GET", "/api/agendamentos");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var hoje = DateTime.UtcNow.Date;
        Assert.All((await r.Json()).EnumerateArray(),
            a => Assert.InRange(a.GetProperty("inicio").GetDateTime().ToUniversalTime(), hoje, hoje.AddDays(7)));
    }

    [Theory]
    [InlineData("de=2099-01-10")] // de malformado
    [InlineData("de=2099-01-10T00:00:00")] // de sem fuso
    [InlineData("de=2099-01-10T00:00:00Z&ate=2099-01-09T00:00:00Z")] // ate antes de de
    [InlineData("de=2099-01-01T00:00:00Z&ate=2099-06-01T00:00:00Z")] // período longo demais
    [InlineData("prestador_id=123")] // prestador não uuid
    public async Task ListarInvalido(string query)
    {
        await using var api = await Servidor.SemBanco();
        await (await api.Chamar("GET", "/api/agendamentos?" + query)).ExigirErro(HttpStatusCode.BadRequest, Erros.RequisicaoInvalida);
    }

    [Fact]
    public async Task Criar()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var api = await Servidor.ComBanco(Seed.Alfa);
        Task<HttpResponseMessage> Criar(string corpo) => api.Chamar("POST", "/api/agendamentos", corpo);

        // Sem fim: duração do Corte (30 min). Preço do Corte no seed: 4500.
        var r = await Criar($$"""{"cliente_id":"{{Seed.ClienteAlfa}}","inicio":"2099-02-01T06:00:00-03:00"}""");
        Assert.True(r.StatusCode == HttpStatusCode.Created, $"criar: {r.StatusCode} {await r.Content.ReadAsStringAsync(ct)}");
        var a = await r.Json();
        Assert.Equal("2099-02-01T09:00:00Z", a.GetProperty("inicio").GetString());
        Assert.Equal("2099-02-01T09:30:00Z", a.GetProperty("fim").GetString());
        Assert.Equal(4500, a.GetProperty("valor_centavos").GetInt32());
        Assert.Equal(Seed.Prestador, a.GetProperty("prestador_id").GetGuid());
        Assert.Equal(Seed.ServicoAlfa, a.GetProperty("servico_id").GetGuid());
        Assert.Equal("Andre Cliente Alfa", a.GetProperty("cliente_nome").GetString());
        Assert.Equal("confirmado", a.GetProperty("status").GetString());
        var id = a.GetProperty("id").GetGuid();

        // Consecutivo: 9h30–10h logo depois de 9h–9h30.
        r = await Criar($$"""{"cliente_id":"{{Seed.ClienteAlfa}}","inicio":"2099-02-01T09:30:00Z","fim":"2099-02-01T10:00:00Z"}""");
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        // Preço congelado: mudar o serviço depois não mexe no agendamento.
        await api.Db.Database.ExecuteSqlAsync($"UPDATE servicos SET preco_centavos = 9999 WHERE id = {Seed.ServicoAlfa}", ct);
        Assert.Equal(4500, (await api.Db.BuscarAgendamento(Seed.Alfa, id, ct))!.ValorCentavos);

        // Cliente do Beta: o Alfa nem enxerga.
        r = await Criar($$"""{"cliente_id":"{{Seed.ClienteBeta}}","inicio":"2099-02-02T09:00:00Z"}""");
        await r.ExigirErro(HttpStatusCode.NotFound, Erros.NaoEncontrado);
        Assert.Equal("cliente_id", (await r.Json()).GetProperty("erro").GetProperty("detalhes").GetProperty("campo").GetString());

        // Por último: a violação 23P01 aborta a transação.
        r = await Criar($$"""{"cliente_id":"{{Seed.ClienteAlfa}}","inicio":"2099-02-01T09:15:00Z"}""");
        await r.ExigirErro(HttpStatusCode.Conflict, Erros.ConflitoHorario);
        Assert.Equal(Seed.Prestador, (await r.Json()).GetProperty("erro").GetProperty("detalhes").GetProperty("prestador_id").GetGuid());
    }

    // Com COMMIT de verdade, no tenant do seed: o fim do teste apaga o que gravou.
    [Fact]
    public async Task CriarSimultaneosNoMesmoHorarioUmSoEntra()
    {
        await using var dono = await Banco.Conectar("MIGRATIONS_DATABASE_URL");
        await using var api = await Servidor.ComPublico(Banco.ConnectionString("DATABASE_URL"));
        var inicio = new DateTime(2098, 3, 1, 9, 0, 0, DateTimeKind.Utc);
        try
        {
            var respostas = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
                api.Chamar("POST", "/api/agendamentos", $$"""{"cliente_id":"{{Seed.ClienteAlfa}}","inicio":"2098-03-01T09:00:00Z"}""")));
            Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Created);
            foreach (var r in respostas.Where(r => r.StatusCode != HttpStatusCode.Created))
            {
                await r.ExigirErro(HttpStatusCode.Conflict, Erros.ConflitoHorario);
            }
            Assert.Equal(1, await dono.Escalar<int>("SELECT count(*) FROM agendamentos WHERE tenant_id = $1 AND inicio = $2", Seed.Alfa, inicio));
        }
        finally
        {
            await dono.Exec("DELETE FROM agendamentos WHERE tenant_id = $1 AND inicio = $2", Seed.Alfa, inicio);
        }
    }

    [Fact]
    public async Task CriarComPrestadorRemovido()
    {
        await using var api = await Servidor.ComBanco(Seed.Alfa);
        await api.Db.Database.ExecuteSqlAsync(
            $"UPDATE memberships SET removido_em = now() WHERE usuario_id = {Seed.Prestador} AND tenant_id = {Seed.Alfa}",
            TestContext.Current.CancellationToken);
        var r = await api.Chamar("POST", "/api/agendamentos", $$"""{"cliente_id":"{{Seed.ClienteAlfa}}","inicio":"2099-02-03T09:00:00Z"}""");
        await r.ExigirErro(HttpStatusCode.NotFound, Erros.NaoEncontrado);
    }

    private const string C = "\"cliente_id\":\"11111111-0004-0000-0000-000000000001\"";

    [Theory]
    [InlineData("""{"inicio":"2099-02-01T09:00:00Z"}""")] // sem cliente
    [InlineData("""{"cliente_id":"1","inicio":"2099-02-01T09:00:00Z"}""")] // cliente não uuid
    [InlineData("{" + C + "}")] // sem inicio
    [InlineData("{" + C + ""","inicio":"2099-02-01T09:00:00"}""")] // inicio sem fuso
    [InlineData("{" + C + ""","inicio":"2099-02-01T09:00:00Z","fim":"2099-02-01T08:00:00Z"}""")] // fim antes do inicio
    [InlineData("{" + C + ""","inicio":"2099-02-01T09:00:00Z","fim":"2099-02-01T09:00:00Z"}""")] // fim igual ao inicio
    [InlineData("{" + C + ""","inicio":"2099-02-01T09:00:00Z","fim":"2099-02-02T10:00:00Z"}""")] // mais de 24h
    [InlineData("{" + C + ""","inicio":"2099-02-01T09:00:00Z","valor_centavos":1}""")] // valor no payload
    public async Task CriarInvalido(string corpo)
    {
        await using var api = await Servidor.SemBanco();
        await (await api.Chamar("POST", "/api/agendamentos", corpo)).ExigirErro(HttpStatusCode.BadRequest, Erros.RequisicaoInvalida);
    }
}
