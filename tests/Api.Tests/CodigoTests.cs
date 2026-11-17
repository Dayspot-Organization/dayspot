using System.Text.RegularExpressions;

namespace Dayspot.Api.Tests;

public static class Repositorio
{
    /// <summary>A raiz do repositório: o primeiro diretório acima que tem o Dayspot.slnx.</summary>
    public static readonly string Raiz = AcharRaiz();

    private static string AcharRaiz()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dayspot.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("raiz do repositório não encontrada");
    }

    /// <summary>
    /// Os arquivos de código sob dir. Pula saída de build (bin, obj), node_modules e
    /// os diretórios ocultos (.git e afins).
    /// </summary>
    public static IEnumerable<string> Arquivos(string dir) =>
        Directory.EnumerateFiles(dir).Concat(Directory.EnumerateDirectories(dir)
            .Where(d => Path.GetFileName(d) is not ("bin" or "obj" or "node_modules") && !Path.GetFileName(d).StartsWith('.'))
            .SelectMany(Arquivos));
}

public partial class CodigoTests
{
    // Critério da 1.3: o tenant só entra por set_config('app.tenant_id', ..., true).
    // SET (ou set_config com false) vale para a conexão e vaza para a próxima
    // requisição do pool; SET LOCAL não aceita parâmetro. Varre o código (.cs e
    // .sql): a documentação cita o SET justamente para proibir.
    [GeneratedRegex(@"\bSET\s+(SESSION\s+|LOCAL\s+)?app\.tenant_id|set_config\(\s*'app\.tenant_id'[^)]*,\s*false\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex TenantSemEscopo();

    [Fact]
    public void SemSetTenantForaDoSetConfigLocal()
    {
        var arquivos = Repositorio.Arquivos(Repositorio.Raiz)
            .Where(a => Path.GetExtension(a) is ".cs" or ".sql").ToList();
        // A varredura alcança a API e as migrations.
        Assert.Contains(arquivos, a => a.EndsWith("AppDb.cs"));
        Assert.Contains(arquivos, a => a.EndsWith("00000000000009_AtivarRls.cs"));
        foreach (var a in arquivos)
        {
            var achados = TenantSemEscopo().Matches(File.ReadAllText(a)).Select(m => m.Value).ToList();
            Assert.True(achados.Count == 0, $"{a}: {string.Join("; ", achados)}; use set_config('app.tenant_id', $1, true)");
        }
    }
}
