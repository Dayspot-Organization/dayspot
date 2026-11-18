using System.Globalization;
using Npgsql;

namespace Dayspot.Api;

/// <summary>Erro de configuração: a mensagem cita cada variável com problema.</summary>
public sealed class ErroDeConfig(string mensagem) : Exception(mensagem);

/// <summary>
/// Config da API, lida uma vez na inicialização (1.1). É o único lugar que lê
/// variáveis de ambiente: o resto recebe a Config pronta. Variáveis em .env.example.
/// </summary>
public sealed record Config(
    // Conecta como app_user, sem BYPASSRLS (RNF07), nunca como o dono das tabelas
    // (MIGRATIONS_DATABASE_URL).
    string DatabaseUrl,
    string PortaHttp,
    // Sessão da Fatia 1, enquanto não há login; saem na 2.12 (docs/requisitos.md §12).
    Guid TenantFixo, // TODO(T2): remover
    Guid PrestadorFixo, // TODO(T2): remover
                        // Serviço da Fatia 1, enquanto não há escolha de serviço; sai na 6.6.
    Guid ServicoFixo) // TODO(T6): remover
{
    private const string UsuarioApi = "app_user"; // criado por db/init/app_user.sh
    private const string PortaPadrao = "8080"; // alvo do proxy do ng serve (web/proxy.conf.json)

    /// <summary>
    /// Lê o .env do diretório atual, se existir, e depois o ambiente. Variável já
    /// definida no ambiente vence o .env (no CI não há .env). Devolve todos os
    /// problemas de uma vez, cada um com o nome da variável.
    /// </summary>
    public static Config Load()
    {
        var arquivo = LerEnv(".env");
        string Getenv(string nome) =>
            Environment.GetEnvironmentVariable(nome) ?? arquivo.GetValueOrDefault(nome, "");

        var erros = new List<string>();
        string Obrigatoria(string nome)
        {
            var v = Getenv(nome);
            if (v == "")
            {
                erros.Add($"{nome} ausente (ver .env.example)");
            }
            return v;
        }
        Guid Id(string nome)
        {
            var v = Obrigatoria(nome);
            if (v == "")
            {
                return Guid.Empty;
            }
            // Só o formato com hífens; o Length barra o espaço que o TryParseExact apararia.
            var id = Guid.Empty;
            if (v.Length != 36 || !Guid.TryParseExact(v, "D", out id))
            {
                erros.Add($"{nome} não é um UUID válido: \"{v}\"");
            }
            else if (id == Guid.Empty)
            {
                erros.Add($"{nome} não pode ser o UUID nulo");
            }
            return id;
        }

        var c = new Config(Obrigatoria("DATABASE_URL"), Getenv("PORTA_HTTP"),
            Id("TENANT_FIXO"), Id("PRESTADOR_FIXO"), Id("SERVICO_FIXO"));

        if (c.DatabaseUrl != "")
        {
            try
            {
                var usuario = new NpgsqlConnectionStringBuilder(ParaConnectionString(c.DatabaseUrl)).Username;
                if (usuario != UsuarioApi)
                {
                    erros.Add($"DATABASE_URL conecta como \"{usuario}\"; a API usa só o {UsuarioApi} (RNF07)");
                }
            }
            catch (FormatException)
            {
                // Sem o texto da exceção: ele pode citar a URL, que tem a senha.
                erros.Add("DATABASE_URL inválida: use postgres://usuario:senha@host:porta/banco");
            }
        }

        if (c.PortaHttp == "")
        {
            c = c with { PortaHttp = PortaPadrao };
        }
        else if (!int.TryParse(c.PortaHttp, NumberStyles.None, CultureInfo.InvariantCulture, out var p) || p < 1 || p > 65535)
        {
            erros.Add($"PORTA_HTTP inválida: \"{c.PortaHttp}\"");
        }

        if (erros.Count > 0)
        {
            throw new ErroDeConfig("config:\n" + string.Join("\n", erros));
        }
        return c;
    }

    /// <summary>
    /// Connection string do dono das tabelas (MIGRATIONS_DATABASE_URL), do ambiente
    /// ou do .env dado. Só o dotnet ef a lê, para as migrations; a API nunca.
    /// </summary>
    public static string MigrationsConnectionString(string env)
    {
        const string nome = "MIGRATIONS_DATABASE_URL";
        var url = Environment.GetEnvironmentVariable(nome) ?? LerEnv(env).GetValueOrDefault(nome, "");
        if (url == "")
        {
            throw new ErroDeConfig($"config:\n{nome} ausente (ver .env.example)");
        }
        return ParaConnectionString(url);
    }

    /// <summary>
    /// Converte a URL postgres://usuario:senha@host:porta/banco?sslmode=... (a mesma
    /// do psql) na connection string do Npgsql, que não lê URL.
    /// </summary>
    public static string ParaConnectionString(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u) || u.Scheme is not ("postgres" or "postgresql"))
        {
            throw new FormatException("URL do banco inválida");
        }
        var credencial = u.UserInfo.Split(':', 2);
        var b = new NpgsqlConnectionStringBuilder
        {
            Host = u.Host,
            Port = u.IsDefaultPort || u.Port < 0 ? 5432 : u.Port,
            Database = Uri.UnescapeDataString(u.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credencial[0]),
            // Sem Kerberos: senão o Npgsql tenta carregar a libgssapi, ausente na imagem.
            GssEncryptionMode = GssEncryptionMode.Disable,
        };
        if (credencial.Length == 2)
        {
            b.Password = Uri.UnescapeDataString(credencial[1]);
        }
        foreach (var par in u.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = par.Split('=', 2);
            var valor = kv.Length == 2 ? Uri.UnescapeDataString(kv[1]) : "";
            try
            {
                switch (kv[0])
                {
                    case "sslmode":
                        b.SslMode = Enum.Parse<SslMode>(valor.Replace("-", ""), ignoreCase: true);
                        break;
                    case "connect_timeout":
                        b.Timeout = int.Parse(valor);
                        break;
                    default:
                        throw new FormatException($"parâmetro {kv[0]} não suportado na URL do banco");
                }
            }
            catch (Exception e) when (e is ArgumentException or OverflowException)
            {
                throw new FormatException($"parâmetro {kv[0]} inválido na URL do banco");
            }
        }
        return b.ConnectionString;
    }

    /// <summary>
    /// Lê o .env sem exportar para o processo: o arquivo também tem a senha do dono
    /// das tabelas. Ausente, devolve vazio. Tira o BOM (o PowerShell 5.1 grava com
    /// ele). O erro não cita o conteúdo da linha, que pode ter senha.
    /// </summary>
    private static Dictionary<string, string> LerEnv(string caminho)
    {
        var vars = new Dictionary<string, string>();
        if (!File.Exists(caminho))
        {
            return vars;
        }
        string[] linhas;
        try
        {
            linhas = File.ReadAllLines(caminho); // já tira o BOM
        }
        catch (IOException e)
        {
            throw new ErroDeConfig($"config: lendo {caminho}: {e.Message}");
        }
        foreach (var crua in linhas)
        {
            var linha = crua.Trim();
            if (linha == "" || linha.StartsWith('#'))
            {
                continue;
            }
            if (linha.StartsWith("export "))
            {
                linha = linha["export ".Length..].TrimStart();
            }
            var igual = linha.IndexOf('=');
            if (igual <= 0)
            {
                throw Malformado(caminho);
            }
            var nome = linha[..igual].TrimEnd();
            var valor = linha[(igual + 1)..].TrimStart();
            if (valor.Length > 0 && valor[0] is '"' or '\'')
            {
                var fecha = valor.IndexOf(valor[0], 1);
                if (fecha < 0)
                {
                    throw Malformado(caminho);
                }
                valor = valor[1..fecha];
            }
            else
            {
                var comentario = valor.IndexOf(" #", StringComparison.Ordinal);
                valor = (comentario >= 0 ? valor[..comentario] : valor).TrimEnd();
            }
            vars[nome] = valor;
        }
        return vars;
    }

    private static ErroDeConfig Malformado(string caminho) =>
        new($"config: {caminho} malformado (aspas sem fechar?); compare com o .env.example");
}
