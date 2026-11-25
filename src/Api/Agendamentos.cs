using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dayspot.Api.Dados;

namespace Dayspot.Api;

/// <summary>
/// Datas sempre em UTC ("...Z"); a conversão para o fuso do tenant é da borda (6.4).
/// </summary>
public sealed record AgendamentoJson(Guid Id, Guid ClienteId, string ClienteNome, Guid ServicoId, Guid PrestadorId,
    DateTime Inicio, DateTime Fim, string Status, int ValorCentavos)
{
    public AgendamentoJson(Agendamento a, string clienteNome) : this(a.Id, a.ClienteId, clienteNome, a.ServicoId,
        a.PrestadorId, a.Inicio.ToUniversalTime(), a.Fim.ToUniversalTime(), a.Status, a.ValorCentavos)
    { }
}

public sealed record NovoAgendamentoJson(string? ClienteId, string? Inicio, string? Fim);

public static partial class Agendamentos
{
    private static readonly TimeSpan PeriodoPadrao = TimeSpan.FromDays(7);
    private static readonly TimeSpan PeriodoMaximo = TimeSpan.FromDays(92); // um trimestre; o calendário (7.1) pede uma semana
    private static readonly TimeSpan DuracaoMaxima = TimeSpan.FromHours(24); // a mesma da duração do serviço (guia, §005)

    // Faixa aceita em toda data da API: longe das pontas do DateTime, então somar o período,
    // a duração ou o fuso não estoura.
    internal static readonly DateTime DataMinima = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    internal static readonly DateTime DataMaxima = new(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// GET /api/agendamentos?de=&amp;ate=&amp;prestador_id=, período [de, ate) pelo inicio,
    /// em RFC 3339. Sem de, hoje; sem ate, de + 7 dias (e vice-versa). Sem
    /// prestador_id, a empresa toda: restringir ao prestador da sessão é da 2.7.
    /// </summary>
    public static async Task<IResult> Listar(HttpRequest req, AppDb db, [FromServices] TimeProvider relogio, CancellationToken ct)
    {
        if (!LerData(req.Query["de"].FirstOrDefault(), "de", out var de, out var erro)
            || !LerData(req.Query["ate"].FirstOrDefault(), "ate", out var ate, out erro))
        {
            return erro!;
        }
        // TODO(T6): "hoje" em UTC; passa a ser no fuso do tenant na 6.4.
        var inicio = de ?? (ate - PeriodoPadrao) ?? relogio.GetUtcNow().UtcDateTime.Date;
        var fim = ate ?? inicio + PeriodoPadrao;
        if (fim < inicio)
        {
            return Erros.CampoInvalido("ate", "A data final é anterior à inicial.");
        }
        if (fim - inicio > PeriodoMaximo)
        {
            return Erros.CampoInvalido("ate", "Período maior que 92 dias.");
        }
        Guid? prestadorId = null;
        if (req.Query["prestador_id"].FirstOrDefault() is { Length: > 0 } v)
        {
            if (!Guid.TryParse(v, out var id))
            {
                return Erros.CampoInvalido("prestador_id", "prestador_id não é um UUID válido.");
            }
            prestadorId = id;
        }

        var agendamentos = await db.ListarAgendamentos(TenantDaSessao.Ler(req.HttpContext), inicio, fim, prestadorId, ct);
        return Json.Responder(StatusCodes.Status200OK,
            agendamentos.Select(a => new AgendamentoJson(a.Agendamento, a.ClienteNome)));
    }

    /// <summary>
    /// POST /api/agendamentos {cliente_id, inicio, fim?}, datas em RFC 3339. Sem fim,
    /// vale a duração do serviço. Prestador e serviço são os fixos da Fatia 1; o preço
    /// vem do serviço, nunca do corpo.
    /// </summary>
    public static async Task<IResult> Criar(HttpRequest req, AppDb db, Fixos fixos, CancellationToken ct)
    {
        var (entrada, erroCorpo) = await Json.Ler<NovoAgendamentoJson>(req, ct);
        if (entrada is null)
        {
            return erroCorpo;
        }
        if (!Guid.TryParse(entrada.ClienteId, out var clienteId))
        {
            return Erros.CampoInvalido("cliente_id", "Informe o cliente.");
        }
        if (string.IsNullOrEmpty(entrada.Inicio))
        {
            return Erros.CampoInvalido("inicio", "Informe o início.");
        }
        if (!LerData(entrada.Inicio, "inicio", out var inicio, out var erro)
            || !LerData(entrada.Fim, "fim", out var fim, out erro))
        {
            return erro!;
        }
        if (fim is not null && (fim <= inicio || fim - inicio > DuracaoMaxima))
        {
            return Erros.CampoInvalido("fim", "O fim deve ser depois do início, em até 24 horas.");
        }

        // Cliente antes, para dizer qual campo falhou: outro tenant ou removido = 404.
        var tenant = TenantDaSessao.Ler(req.HttpContext);
        var cliente = await db.BuscarCliente(tenant, clienteId, ct);
        if (cliente is null)
        {
            return Erros.Responder(StatusCodes.Status404NotFound, Erros.NaoEncontrado, "Cliente não encontrado.",
                new() { ["campo"] = "cliente_id" });
        }
        Agendamento? a;
        try
        {
            a = await db.CriarAgendamento(tenant,
                new NovoAgendamento(clienteId, fixos.ServicoId, fixos.PrestadorId, inicio!.Value, fim), ct);
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.ExclusionViolation)
        {
            // A constraint sem_sobreposicao barrou (SQLSTATE 23P01).
            return Erros.Responder(StatusCodes.Status409Conflict, Erros.ConflitoHorario, "Já existe um agendamento nesse horário.",
                new() { ["prestador_id"] = fixos.PrestadorId });
        }
        if (a is null)
        {
            // Prestador sem membership ativo ou serviço inexistente/inativo.
            return Erros.Responder(StatusCodes.Status404NotFound, Erros.NaoEncontrado, "Prestador ou serviço indisponível.");
        }
        return Json.Responder(StatusCodes.Status201Created, new AgendamentoJson(a, cliente.Nome));
    }

    /// <summary>
    /// Lê uma data RFC 3339 (ISO 8601 com fuso) e devolve em UTC. Vazia devolve nulo.
    /// Em erro, devolve false com a resposta 400 em erro.
    /// </summary>
    internal static bool LerData(string? v, string campo, out DateTime? data, out IResult? erro)
    {
        data = null;
        erro = null;
        if (string.IsNullOrEmpty(v))
        {
            return true;
        }
        // A regex exige o formato com fuso; a fração é cortada nos 7 dígitos que o DateTime guarda.
        var m = Rfc3339().Match(v);
        var fracao = m.Groups[2].Value;
        if (m.Success && DateTimeOffset.TryParse(m.Groups[1].Value + fracao[..Math.Min(fracao.Length, 8)] + m.Groups[3].Value,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var lida))
        {
            if (lida.UtcDateTime < DataMinima || lida.UtcDateTime >= DataMaxima)
            {
                erro = Erros.CampoInvalido(campo, campo + " fora da faixa aceita (anos 2000 a 2099).");
                return false;
            }
            data = lida.UtcDateTime;
            return true;
        }
        erro = Erros.CampoInvalido(campo, campo + " deve estar em ISO 8601 com fuso, ex.: 2026-10-16T09:00:00-03:00.");
        return false;
    }

    [GeneratedRegex(@"^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})(\.\d+)?(Z|[+-]\d{2}:\d{2})$")]
    private static partial Regex Rfc3339();
}
