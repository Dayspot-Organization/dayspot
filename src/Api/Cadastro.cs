using System.Text.RegularExpressions;
using Dayspot.Api.Auth;
using Dayspot.Api.Dados;
using Microsoft.EntityFrameworkCore;

namespace Dayspot.Api;

public sealed record NovoCadastroJson(string? Nome, string? Email, string? Senha, string? EmpresaNome, string? EmpresaSlug, string? Contato);

public sealed record CadastroJson(Guid UsuarioId, Guid TenantId, string Slug, string Papel);

/// <summary>
/// Cadastro de empresa e do primeiro administrador (#112). Roda dentro da transação
/// por requisição (UseTransacaoNasRotas, com TENANT_FIXO): usuarios, tenants e
/// memberships ficam fora do RLS (guia §009), então o tenant não importa, e um 4xx
/// desfaz a transação.
/// </summary>
public static partial class Cadastro
{
    public const int MaxNome = 200;
    public const int MinSenha = 8;
    public const int MaxEmpresaNome = 120;
    public const int MaxContato = 120;
    public const int MaxEmail = 254;

    // Slug já em minúsculas: 3 a 50 caracteres, sem hífen nas pontas.
    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,48}[a-z0-9]$")]
    private static partial Regex PadraoSlug();

    public static async Task<IResult> Criar(HttpContext ctx, AppDb db, CancellationToken ct)
    {
        var entrada = await Json.Ler<NovoCadastroJson>(ctx.Request, ct);
        if (entrada is null)
        {
            return Json.CorpoInvalido();
        }

        var nome = (entrada.Nome ?? "").Trim();
        if (nome == "" || Clientes.Caracteres(nome) > MaxNome)
        {
            return Erros.CampoInvalido("nome", "Informe o nome (até 200 caracteres).");
        }
        var email = Email.Normalizar(entrada.Email ?? "");
        if (!EmailValido(email))
        {
            return Erros.CampoInvalido("email", "Informe um e-mail válido (até 254 caracteres).");
        }
        // A senha não sofre Trim: espaço nas pontas faz parte dela.
        var senha = entrada.Senha ?? "";
        if (Clientes.Caracteres(senha) < MinSenha)
        {
            return Erros.CampoInvalido("senha", "Senha com no mínimo 8 caracteres.");
        }
        var empresaNome = (entrada.EmpresaNome ?? "").Trim();
        if (empresaNome == "" || Clientes.Caracteres(empresaNome) > MaxEmpresaNome)
        {
            return Erros.CampoInvalido("empresa_nome", "Informe o nome da empresa (até 120 caracteres).");
        }
        var slug = (entrada.EmpresaSlug ?? "").Trim().ToLowerInvariant();
        if (!PadraoSlug().IsMatch(slug))
        {
            return Erros.CampoInvalido("empresa_slug",
                "Endereço com 3 a 50 caracteres: letras, números e hífen, sem hífen nas pontas.");
        }
        var contato = (entrada.Contato ?? "").Trim();
        if (contato == "" || Clientes.Caracteres(contato) > MaxContato)
        {
            return Erros.CampoInvalido("contato", "Informe o contato da empresa (até 120 caracteres).");
        }

        // Provisório: a gravação entra na Tarefa 4. Até lá, só garante que a validação
        // passou tocando no banco.
        await db.Database.OpenConnectionAsync(ct);
        return Json.Responder(StatusCodes.Status501NotImplemented, new { });
    }

    // Exatamente um @, com texto dos dois lados.
    private static bool EmailValido(string email)
    {
        if (email.Length > MaxEmail)
        {
            return false;
        }
        var arroba = email.IndexOf('@');
        return arroba > 0 && arroba < email.Length - 1 && email.IndexOf('@', arroba + 1) < 0;
    }
}
