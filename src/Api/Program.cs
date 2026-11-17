using Microsoft.EntityFrameworkCore;
using Dayspot.Api;
using Dayspot.Api.Dados;

// Fixos no código (1.14); vão para o config só se alguém precisar mudar.
var timeoutCabecalhos = TimeSpan.FromSeconds(5); // corta slowloris
var timeoutOcioso = TimeSpan.FromSeconds(60);
var timeoutPing = TimeSpan.FromSeconds(5);
var timeoutShutdown = TimeSpan.FromSeconds(30); // dá tempo de a requisição mais longa terminar

Config cfg;
try
{
    cfg = Config.Load();
}
catch (ErroDeConfig e)
{
    // Sem config válida a API não sobe (1.1): a mensagem diz qual variável.
    Console.Error.WriteLine(e.Message);
    return 1;
}

var builder = WebApplication.CreateSlimBuilder(args);
// Sem uma linha de log por requisição e por query: só avisos e erros do framework.
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.WebHost.ConfigureKestrel(k =>
{
    k.ListenAnyIP(int.Parse(cfg.PortaHttp));
    k.Limits.RequestHeadersTimeout = timeoutCabecalhos;
    k.Limits.KeepAliveTimeout = timeoutOcioso;
});
// Shutdown gracioso: para de aceitar conexões e espera as requisições em andamento.
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = timeoutShutdown);
// Pool do app_user: o config já recusou outro usuário (RNF07).
builder.Services.AddSingleton(AppDb.Opcoes(Config.ParaConnectionString(cfg.DatabaseUrl)));
builder.Services.AddScoped<AppDb>();
builder.Services.AddSingleton(new Fixos(cfg.TenantFixo, cfg.PrestadorFixo, cfg.ServicoFixo));

var app = builder.Build();

// O pool conecta sob demanda: sem o ping, banco fora do ar só aparece como 500.
using (var escopo = app.Services.CreateScope())
using (var prazo = new CancellationTokenSource(timeoutPing))
{
    try
    {
        var db = escopo.ServiceProvider.GetRequiredService<AppDb>();
        await db.Database.OpenConnectionAsync(prazo.Token);
        await db.Database.ExecuteSqlRawAsync("SELECT 1", prazo.Token);
    }
    catch (Exception e)
    {
        app.Logger.LogError(e, "não alcançou o banco");
        return 1;
    }
}

app.UseRouting();
// Toda requisição numa transação com o tenant; na Fatia 2 ele vem da sessão.
app.UseTransacaoNasRotas(_ => cfg.TenantFixo);
app.MapApi();

app.Logger.LogInformation("API no ar na porta {Porta}, tenant fixo {TenantFixo}", cfg.PortaHttp, cfg.TenantFixo);
await app.RunAsync();
app.Logger.LogInformation("API desligada");
return 0;
