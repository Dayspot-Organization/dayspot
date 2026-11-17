namespace Dayspot.Api;

/// <summary>
/// Sessão da Fatia 1, enquanto não há login: TENANT_FIXO e PRESTADOR_FIXO (saem na
/// 2.12, quando o tenant passa a vir da sessão) e SERVICO_FIXO (sai na 6.6). Os
/// handlers recebem pelo contêiner de serviços.
/// </summary>
public sealed record Fixos(Guid TenantId, Guid PrestadorId, Guid ServicoId);

public static class Rotas
{
    // HEAD junto com GET: o corpo não sai, mas a rota responde como no GET.
    private static readonly string[] Leitura = ["GET", "HEAD"];

    /// <summary>
    /// Toda rota fica sob /api (requisitos §12) e todo erro sai no formato de
    /// docs/erros-api.md.
    /// </summary>
    public static void MapApi(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/clientes", Clientes.Criar);
        app.MapMethods("/api/clientes", Leitura, Clientes.Listar);
        app.MapMethods("/api/agendamentos", Leitura, Agendamentos.Listar);
        app.MapPost("/api/agendamentos", Agendamentos.Criar);
    }
}
