using System.Collections.Frozen;
using System.Globalization;
using System.Text;

namespace Dayspot.Api.Auth;

/// <summary>
/// Regra da senha nova (#211, NIST SP 800-63B): comprimento e lista de senhas comuns, sem
/// exigir maiúscula, número ou símbolo. A mesma regra está no front (web/src/app/entrada/senha.ts).
/// Vale só para senha nova: o login confere qualquer senha antiga.
/// </summary>
public static class ForcaSenha
{
    public const int Minimo = 10;
    public const int Maximo = 1024;
    // Parte do nome ou do e-mail com menos que isso não barra: "Ana" não barra "banana".
    private const int MinParte = 4;

    // web/src/senhas-comuns.txt, embutida pelo Api.csproj. Já em minúsculas e com 10+ caracteres.
    private static readonly FrozenSet<string> Comuns = Carregar();

    /// <summary>O motivo da recusa, para o erro no campo senha; null = senha aceita.</summary>
    public static string? Motivo(string senha, string nome, string email)
    {
        var tamanho = senha.EnumerateRunes().Count();
        if (tamanho < Minimo)
        {
            return "Use pelo menos 10 caracteres.";
        }
        if (tamanho > Maximo)
        {
            return "Use até 1024 caracteres.";
        }
        var s = Normalizar(senha);
        if (Comuns.Contains(s))
        {
            return "Essa senha é muito usada. Escolha outra.";
        }
        var partes = Normalizar(nome).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Append(Normalizar(email.Split('@')[0]));
        if (partes.Any(p => p.Length >= MinParte && s.Contains(p, StringComparison.Ordinal)))
        {
            return "A senha não pode ter seu nome ou seu e-mail.";
        }
        return null;
    }

    /// <summary>Minúsculas e sem acento, como o front: "JOÃO" e "joao" são iguais.</summary>
    private static string Normalizar(string texto)
    {
        var sb = new StringBuilder(texto.Length);
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().ToLowerInvariant();
    }

    private static FrozenSet<string> Carregar()
    {
        using var arquivo = typeof(ForcaSenha).Assembly.GetManifestResourceStream("senhas-comuns.txt")
            ?? throw new InvalidOperationException("senhas-comuns.txt não embutida (Api.csproj)");
        using var leitor = new StreamReader(arquivo);
        return leitor.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => !l.StartsWith('#')).ToFrozenSet();
    }
}
