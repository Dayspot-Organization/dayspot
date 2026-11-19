using Dayspot.Api.Dados;

namespace Dayspot.Api;

/// <summary>
/// O que a API mostra do cliente. Sem vinculo_token: é credencial do Telegram (guia
/// de banco, §004) e só sai pela rota de vínculo (Fatia 4).
/// </summary>
public sealed record ClienteJson(Guid Id, string Nome, string? Telefone, DateTime CriadoEm)
{
    public ClienteJson(Cliente c) : this(c.Id, c.Nome, c.Telefone, c.CriadoEm) { }
}

public sealed record NovoClienteJson(string? Nome, string? Telefone);

public static class Clientes
{
    internal const int MaxNome = 200;
    internal const int MaxTelefone = 30;

    public static async Task<IResult> Criar(HttpRequest req, AppDb db, Fixos fixos, CancellationToken ct)
    {
        var entrada = await Json.Ler<NovoClienteJson>(req, ct);
        if (entrada is null)
        {
            return Json.CorpoInvalido();
        }
        var nome = (entrada.Nome ?? "").Trim();
        if (nome == "" || Caracteres(nome) > MaxNome)
        {
            return Erros.CampoInvalido("nome", "Informe o nome (até 200 caracteres).");
        }
        var telefone = entrada.Telefone?.Trim();
        if (telefone is not null && Caracteres(telefone) > MaxTelefone)
        {
            return Erros.CampoInvalido("telefone", "Telefone com mais de 30 caracteres.");
        }
        if (telefone == "")
        {
            telefone = null; // vazio = sem telefone
        }

        var c = await db.CriarCliente(fixos.TenantId, nome, telefone, ct);
        return Json.Responder(StatusCodes.Status201Created, new ClienteJson(c));
    }

    public static async Task<IResult> Listar(AppDb db, Fixos fixos, CancellationToken ct)
    {
        var clientes = await db.ListarClientes(fixos.TenantId, ct);
        return Json.Responder(StatusCodes.Status200OK, clientes.Select(c => new ClienteJson(c)));
    }

    // Caracteres Unicode, não unidades UTF-16: um emoji conta um.
    internal static int Caracteres(string s) => s.EnumerateRunes().Count();
}
