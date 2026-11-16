using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Konscious.Security.Cryptography;

namespace Dayspot.Api.Auth;

/// <summary>
/// senha_hash fora do formato PHC do Argon2id: dado corrompido no banco, não
/// senha errada.
/// </summary>
public sealed class HashInvalidoException() : Exception("auth: hash de senha em formato inválido");

public static partial class Senha
{
    // Mínimo recomendado pela OWASP para Argon2id. Os parâmetros vão gravados no
    // próprio hash (formato PHC): subir o custo depois não invalida senha antiga.
    private const int MemoriaKiB = 19 * 1024;
    private const int Tempo = 2;
    private const int Threads = 1;
    private const int SaltBytes = 16;
    private const int ChaveBytes = 32;
    private const int Versao = 19; // 0x13, a versão atual do Argon2

    /// <summary>
    /// Hash Argon2id da senha, com salt aleatório, no formato
    /// $argon2id$v=19$m=19456,t=2,p=1$&lt;salt&gt;$&lt;chave&gt; que vai para usuarios.senha_hash.
    /// </summary>
    public static string GerarHash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var chave = Derivar(senha, salt, Tempo, MemoriaKiB, Threads, ChaveBytes);
        return string.Create(CultureInfo.InvariantCulture,
            $"$argon2id$v={Versao}$m={MemoriaKiB},t={Tempo},p={Threads}${B64(salt)}${B64(chave)}");
    }

    /// <summary>
    /// Diz se a senha bate com o hash. Senha errada é false; exceção
    /// (<see cref="HashInvalidoException"/>) só quando o hash não está no formato esperado.
    /// </summary>
    public static bool Conferir(string senha, string hash)
    {
        // "", "argon2id", "v=19", "m=...,t=...,p=...", salt, chave
        var partes = hash.Split('$');
        if (partes.Length != 6 || partes[0] != "" || partes[1] != "argon2id" || partes[2] != $"v={Versao}")
        {
            throw new HashInvalidoException();
        }
        var m = Parametros().Match(partes[3]);
        if (!m.Success
            || !int.TryParse(m.Groups[1].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var memoria)
            || !int.TryParse(m.Groups[2].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var tempo)
            || !byte.TryParse(m.Groups[3].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var threads)
            || memoria == 0 || tempo == 0 || threads == 0)
        {
            throw new HashInvalidoException();
        }
        var salt = DeB64(partes[4]);
        var chave = DeB64(partes[5]);
        if (senha.Length == 0)
        {
            return false; // a biblioteca não deriva senha vazia, e o cadastro não aceita uma
        }

        byte[] calculada;
        try
        {
            calculada = Derivar(senha, salt, tempo, memoria, threads, chave.Length);
        }
        catch (ArgumentException)
        {
            throw new HashInvalidoException(); // parâmetros que a biblioteca recusa
        }
        // Tempo constante: com ==, o tempo de resposta vaza quantos bytes bateram.
        return CryptographicOperations.FixedTimeEquals(calculada, chave);
    }

    private static byte[] Derivar(string senha, byte[] salt, int tempo, int memoriaKiB, int threads, int bytes)
    {
        using var argon = new Argon2id(Encoding.UTF8.GetBytes(senha))
        {
            Salt = salt,
            Iterations = tempo,
            MemorySize = memoriaKiB,
            DegreeOfParallelism = threads,
        };
        return argon.GetBytes(bytes);
    }

    // Base64 padrão sem padding, como no formato PHC.
    private static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=');

    private static byte[] DeB64(string s)
    {
        if (!Base64SemPadding().IsMatch(s))
        {
            throw new HashInvalidoException();
        }
        try
        {
            return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
        }
        catch (FormatException)
        {
            throw new HashInvalidoException();
        }
    }

    [GeneratedRegex(@"^m=(\d+),t=(\d+),p=(\d+)$")]
    private static partial Regex Parametros();

    [GeneratedRegex(@"^[A-Za-z0-9+/]+$")]
    private static partial Regex Base64SemPadding();
}
