using System.Text;
using System.Text.RegularExpressions;

namespace Dayspot.Api.Tests;

// Testes da 1.1. Cada teste monta o próprio ambiente: no CI não há .env nem as
// variáveis do config. O Dispose restaura diretório e variáveis no fim.
public sealed partial class ConfigTests : IDisposable
{
    private static readonly string[] Variaveis = ["DATABASE_URL", "PORTA_HTTP", "TENANT_FIXO", "PRESTADOR_FIXO", "SERVICO_FIXO"];

    // UUIDs do Alfa no seed (db/seed.sql, tabela no README).
    private const string UrlAppUser = "postgres://app_user:x@localhost:5467/example?sslmode=disable";
    private const string TenantAlfa = "11111111-1111-1111-1111-111111111111";
    private const string PrestadorAlfa = "cccccccc-cccc-cccc-cccc-cccccccccccc";
    private const string ServicoCorte = "11111111-0005-0000-0000-000000000001";

    private readonly string _diretorioOriginal = Environment.CurrentDirectory;
    private readonly Dictionary<string, string?> _originais = Variaveis.ToDictionary(v => v, Environment.GetEnvironmentVariable);
    private readonly string _temp = Directory.CreateTempSubdirectory("config-test").FullName;

    // Todo teste começa num diretório sem .env e sem as variáveis do config.
    public ConfigTests()
    {
        Environment.CurrentDirectory = _temp;
        foreach (var v in Variaveis)
        {
            Environment.SetEnvironmentVariable(v, null);
        }
    }

    public void Dispose()
    {
        Environment.CurrentDirectory = _diretorioOriginal;
        foreach (var (v, valor) in _originais)
        {
            Environment.SetEnvironmentVariable(v, valor);
        }
        Directory.Delete(_temp, recursive: true);
    }

    private static void AmbienteValido()
    {
        Environment.SetEnvironmentVariable("DATABASE_URL", UrlAppUser);
        Environment.SetEnvironmentVariable("TENANT_FIXO", TenantAlfa);
        Environment.SetEnvironmentVariable("PRESTADOR_FIXO", PrestadorAlfa);
        Environment.SetEnvironmentVariable("SERVICO_FIXO", ServicoCorte);
    }

    private static void EscreverEnv(string conteudo, bool bom = false) =>
        File.WriteAllText(".env", conteudo, new UTF8Encoding(encoderShouldEmitUTF8Identifier: bom));

    // Falha se Load não der erro ou se a mensagem não citar cada trecho.
    private static void ErroCom(params string[] trechos)
    {
        var erro = Assert.Throws<ErroDeConfig>(() => Config.Load());
        Assert.All(trechos, t => Assert.Contains(t, erro.Message));
    }

    [Fact]
    public void Valido()
    {
        AmbienteValido();
        var c = Config.Load();
        Assert.Equal(UrlAppUser, c.DatabaseUrl);
        Assert.Equal("8080", c.PortaHttp);
        Assert.Equal((Guid.Parse(TenantAlfa), Guid.Parse(PrestadorAlfa), Guid.Parse(ServicoCorte)),
            (c.TenantFixo, c.PrestadorFixo, c.ServicoFixo));
    }

    [Fact]
    public void SemTenantFixo()
    {
        AmbienteValido();
        Environment.SetEnvironmentVariable("TENANT_FIXO", null);
        ErroCom("TENANT_FIXO ausente");
    }

    [Fact]
    public void ListaTodasAsAusentes() =>
        ErroCom("DATABASE_URL ausente", "TENANT_FIXO ausente", "PRESTADOR_FIXO ausente", "SERVICO_FIXO ausente");

    [Theory]
    [InlineData("abc")]
    [InlineData(" " + TenantAlfa)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void UuidInvalido(string valor)
    {
        AmbienteValido();
        Environment.SetEnvironmentVariable("PRESTADOR_FIXO", valor);
        ErroCom("PRESTADOR_FIXO");
    }

    [Fact]
    public void DatabaseUrlDoDono()
    {
        AmbienteValido();
        Environment.SetEnvironmentVariable("DATABASE_URL", "postgres://example:example@localhost:5467/example?sslmode=disable");
        ErroCom("DATABASE_URL", "app_user");
    }

    // A mensagem não pode repetir a URL, que tem a senha.
    [Theory]
    [InlineData("host=localhost user=app_user password=SEGREDO")]
    [InlineData("postgres://app_user:SEGREDO@localhost:5467/example?sslmode=talvez")]
    public void DatabaseUrlInvalidaNaoVazaSegredo(string url)
    {
        AmbienteValido();
        Environment.SetEnvironmentVariable("DATABASE_URL", url);
        var erro = Assert.Throws<ErroDeConfig>(() => Config.Load());
        Assert.Contains("DATABASE_URL inválida", erro.Message);
        Assert.DoesNotContain("SEGREDO", erro.Message);
    }

    [Fact]
    public void UrlViraConnectionString()
    {
        var cs = new Npgsql.NpgsqlConnectionStringBuilder(
            Config.ParaConnectionString("postgres://app_user:s%40nha@db.interno:5467/example?sslmode=disable&connect_timeout=1"));
        Assert.Equal(("db.interno", 5467, "example", "app_user", "s@nha", Npgsql.SslMode.Disable, 1, Npgsql.GssEncryptionMode.Disable),
            (cs.Host, cs.Port, cs.Database, cs.Username, cs.Password, cs.SslMode, cs.Timeout, cs.GssEncryptionMode));
    }

    [Fact]
    public void PortaValida()
    {
        AmbienteValido();
        Environment.SetEnvironmentVariable("PORTA_HTTP", "9090");
        Assert.Equal("9090", Config.Load().PortaHttp);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("70000")]
    public void PortaInvalida(string valor)
    {
        AmbienteValido();
        Environment.SetEnvironmentVariable("PORTA_HTTP", valor);
        ErroCom("PORTA_HTTP");
    }

    [Fact]
    public void LeArquivoEnv()
    {
        EscreverEnv($"DATABASE_URL={UrlAppUser}\nTENANT_FIXO={TenantAlfa}\nPRESTADOR_FIXO={PrestadorAlfa}\nSERVICO_FIXO={ServicoCorte}\nPORTA_HTTP=8081\n");
        var c = Config.Load();
        Assert.Equal(Guid.Parse(TenantAlfa), c.TenantFixo);
        Assert.Equal("8081", c.PortaHttp);
    }

    [Fact]
    public void AmbienteVenceArquivo()
    {
        AmbienteValido();
        EscreverEnv("TENANT_FIXO=22222222-2222-2222-2222-222222222222\n");
        Assert.Equal(Guid.Parse(TenantAlfa), Config.Load().TenantFixo);
    }

    // Erro de .env malformado não pode citar o conteúdo cru: senha nenhuma vai junto.
    [Fact]
    public void EnvMalformadoNaoVazaSegredo()
    {
        EscreverEnv($"DATABASE_URL=\"postgres://app_user:SEGREDO@localhost/x\nTENANT_FIXO={TenantAlfa}\n");
        var erro = Assert.Throws<ErroDeConfig>(() => Config.Load());
        Assert.DoesNotContain("SEGREDO", erro.Message);
        Assert.Contains(".env", erro.Message);
    }

    // PowerShell 5.1 grava com BOM (Out-File, >): o .env tem que valer igual.
    [Fact]
    public void EnvComBom()
    {
        EscreverEnv($"DATABASE_URL={UrlAppUser}\nTENANT_FIXO={TenantAlfa}\nPRESTADOR_FIXO={PrestadorAlfa}\nSERVICO_FIXO={ServicoCorte}\n", bom: true);
        Config.Load();
    }

    // Aspas, comentários e export, como no .env.example.
    [Fact]
    public void EnvComAspasEComentarios()
    {
        EscreverEnv($"# comentário\n\nexport DATABASE_URL=\"{UrlAppUser}\"\nTENANT_FIXO='{TenantAlfa}'\n" +
            $"PRESTADOR_FIXO = {PrestadorAlfa} # o do seed\nSERVICO_FIXO={ServicoCorte}\r\n");
        var c = Config.Load();
        Assert.Equal(UrlAppUser, c.DatabaseUrl);
        Assert.Equal((Guid.Parse(TenantAlfa), Guid.Parse(PrestadorAlfa)), (c.TenantFixo, c.PrestadorFixo));
    }

    // Leituras do ambiente que o guarda abaixo procura.
    [GeneratedRegex(@"\bEnvironment\s*\.\s*(GetEnvironmentVariables?|ExpandEnvironmentVariables)\b")]
    private static partial Regex LeAmbiente();

    [Theory]
    [InlineData("var x = Environment.GetEnvironmentVariable(\"X\");", true)]
    [InlineData("var x = System.Environment\n    .GetEnvironmentVariables();", true)]
    [InlineData("var x = Environment.ExpandEnvironmentVariables(\"%X%\");", true)]
    [InlineData("var x = Environment.CurrentDirectory;", false)]
    [InlineData("var x = cfg.GetEnvironmentVariable;", false)]
    public void LeAmbienteReconhece(string codigo, bool quer) => Assert.Equal(quer, LeAmbiente().IsMatch(codigo));

    // Critério da 1.1: só o Config.cs lê o ambiente. Os testes ficam de fora (leem
    // DATABASE_URL e CI para decidir se pulam).
    [Fact]
    public void SoConfigLeAmbiente()
    {
        var src = Path.Combine(Repositorio.Raiz, "src");
        var arquivos = Repositorio.Arquivos(src)
            .Where(a => Path.GetExtension(a) == ".cs" && Path.GetFileName(a) != "Config.cs").ToList();
        Assert.NotEmpty(arquivos);
        Assert.All(arquivos, a => Assert.False(LeAmbiente().IsMatch(File.ReadAllText(a)),
            $"{a} lê o ambiente direto; use o Config"));
    }
}
