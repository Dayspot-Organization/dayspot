using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dayspot.Api.Tests;

// #210: modelos, envio pelo Resend (com HTTP falso) e 503 quando o envio falha.
public class EnvioEmailTests
{
    private const string Para = "ana@exemplo.test";

    private static readonly Config Cfg = new("postgres://app_user:x@localhost/x", "8080",
        Seed.Alfa, Seed.Prestador, Seed.ServicoAlfa, false, "re_teste", "Dayspot <nao-responda@exemplo.test>");

    [Fact]
    public void ModeloDoCodigo()
    {
        var m = ModelosEmail.CodigoVerificacao(Para, "482913");
        Assert.Equal(Para, m.Para);
        Assert.Contains("482913", m.Assunto);
        Assert.Contains("482913", m.Texto);
        Assert.Contains("482913", m.Html);
        Assert.Contains("10 minutos", m.Texto);
    }

    [Fact]
    public void ModeloDaSenhaEscapaOLink()
    {
        var m = ModelosEmail.RedefinirSenha(Para, "https://dayspot.test/redefinir-senha/abc?x=1&y=\"2\"");
        Assert.Equal(Para, m.Para);
        Assert.Contains("https://dayspot.test/redefinir-senha/abc?x=1&y=\"2\"", m.Texto);
        Assert.Contains("href=\"https://dayspot.test/redefinir-senha/abc?x=1&amp;y=&quot;2&quot;\"", m.Html);
        Assert.Contains("30 minutos", m.Texto);
    }

    // Rota de teste que usa o envio como as rotas do #213 e do #214.
    private static Task<Servidor> ComEnvio(EnvioFalso envio) => Servidor.Criar(
        s => s.AddSingleton<IEnvioEmail>(envio),
        app => app.MapPost("/enviar", async (IEnvioEmail e, CancellationToken ct) =>
            await e.Enviar(ModelosEmail.CodigoVerificacao(Para, "123456"), ct) ? Results.NoContent() : Erros.EmailNaoEnviado()));

    [Fact]
    public async Task EnvioFalsoRecebeModeloEDestinatario()
    {
        var envio = new EnvioFalso();
        await using var api = await ComEnvio(envio);
        Assert.Equal(HttpStatusCode.NoContent, (await api.Chamar("POST", "/enviar")).StatusCode);
        var m = Assert.Single(envio.Enviadas);
        Assert.Equal((Para, "Seu código do Dayspot: 123456"), (m.Para, m.Assunto));
    }

    [Fact]
    public async Task FalhaNoEnvioDa503()
    {
        await using var api = await ComEnvio(new EnvioFalso { Falhar = true });
        var r = await api.Chamar("POST", "/enviar");
        await r.ExigirErro(HttpStatusCode.ServiceUnavailable, Erros.EmailIndisponivel);
        Assert.DoesNotContain("Resend", await r.Content.ReadAsStringAsync());
    }

    private sealed class HttpFalso(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? Pedido { get; private set; }
        public string Corpo { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
        {
            Pedido = req;
            Corpo = await req.Content!.ReadAsStringAsync(ct);
            return await responder(req);
        }
    }

    private static (EnvioResend, LogEmMemoria) Resend(HttpFalso http)
    {
        var log = new LogEmMemoria();
        var cliente = new HttpClient(http) { BaseAddress = new Uri("https://api.resend.com/") };
        return (new EnvioResend(cliente, Cfg, new LoggerFactory([log]).CreateLogger<EnvioResend>()), log);
    }

    [Fact]
    public async Task ResendRecebeOPedidoCerto()
    {
        var http = new HttpFalso(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var (envio, _) = Resend(http);
        Assert.True(await envio.Enviar(ModelosEmail.CodigoVerificacao(Para, "123456"), CancellationToken.None));

        Assert.Equal((HttpMethod.Post, "https://api.resend.com/emails"), (http.Pedido!.Method, http.Pedido.RequestUri!.ToString()));
        Assert.Equal("Bearer re_teste", http.Pedido.Headers.Authorization!.ToString());
        var corpo = JsonDocument.Parse(http.Corpo).RootElement;
        Assert.Equal("Dayspot <nao-responda@exemplo.test>", corpo.GetProperty("from").GetString());
        Assert.Equal(Para, corpo.GetProperty("to")[0].GetString());
        Assert.Equal("Seu código do Dayspot: 123456", corpo.GetProperty("subject").GetString());
        Assert.Contains("123456", corpo.GetProperty("text").GetString());
        Assert.Contains("123456", corpo.GetProperty("html").GetString());
    }

    public static TheoryData<string> Falhas => ["recusa", "rede", "prazo"];

    // Recusa, rede e prazo estourado dão false, e o log não leva destinatário nem código.
    [Theory]
    [MemberData(nameof(Falhas))]
    public async Task FalhaDoResendDaFalseSemVazarNoLog(string falha)
    {
        var http = new HttpFalso(_ => falha switch
        {
            "recusa" => Task.FromResult(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
            { Content = new StringContent($"{{\"message\":\"invalid to {Para}\"}}") }),
            "rede" => throw new HttpRequestException("conexão recusada"),
            _ => throw new TaskCanceledException("prazo"),
        });
        var (envio, log) = Resend(http);
        Assert.False(await envio.Enviar(ModelosEmail.CodigoVerificacao(Para, "123456"), CancellationToken.None));
        Assert.NotEmpty(log.Linhas);
        Assert.All(log.Linhas, l => Assert.DoesNotContain(Para, l));
        Assert.All(log.Linhas, l => Assert.DoesNotContain("123456", l));
    }

    // Envio real, só à mão: RESEND_API_KEY, EMAIL_REMETENTE (domínio verificado) e
    // EMAIL_TESTE (destino) no ambiente. Sem eles, pulado; o CI não tem a chave.
    [Fact]
    public async Task EnvioRealPeloResend()
    {
        var (chave, remetente, destino) = (Environment.GetEnvironmentVariable("RESEND_API_KEY"),
            Environment.GetEnvironmentVariable("EMAIL_REMETENTE"), Environment.GetEnvironmentVariable("EMAIL_TESTE"));
        if (string.IsNullOrEmpty(chave) || string.IsNullOrEmpty(remetente) || string.IsNullOrEmpty(destino))
        {
            Assert.Skip("RESEND_API_KEY, EMAIL_REMETENTE ou EMAIL_TESTE ausente: envio real pulado");
        }
        var servicos = new ServiceCollection().AddLogging().AddEmail(Cfg with { ResendApiKey = chave!, EmailRemetente = remetente! });
        await using var sp = servicos.BuildServiceProvider();
        var envio = sp.GetRequiredService<IEnvioEmail>();
        Assert.IsType<EnvioResend>(envio);
        Assert.True(await envio.Enviar(ModelosEmail.CodigoVerificacao(destino!, "482913"), TestContext.Current.CancellationToken));
    }
}
