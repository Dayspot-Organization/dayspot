using Microsoft.EntityFrameworkCore;

namespace Dayspot.Api.Dados;

/// <summary>O que vem da requisição. Fim nulo = inicio + duração do serviço.</summary>
public sealed record NovoAgendamento(Guid ClienteId, Guid ServicoId, Guid PrestadorId, DateTime Inicio, DateTime? Fim);

public sealed record AgendamentoComCliente(Agendamento Agendamento, string ClienteNome);

/// <summary>
/// As consultas da API. Toda uma recebe tenantId logo depois do contexto e filtra
/// por ele, mesmo com RLS: é a camada (b) de defesa (requisitos §6.2). Rodam dentro
/// da transação aberta por <see cref="AppDb.IniciarComTenant"/>.
/// </summary>
public static class Consultas
{
    // Cliente removido (exclusão lógica) não aparece.
    public static Task<List<Cliente>> ListarClientes(this AppDb db, Guid tenantId, CancellationToken ct = default) =>
        db.Clientes.Where(c => c.TenantId == tenantId && c.RemovidoEm == null)
            .OrderBy(c => c.Nome)
            .ToListAsync(ct);

    /// <summary>Nulo: inexistente, removido ou de outro tenant (o RLS nem deixa ver).</summary>
    public static Task<Cliente?> BuscarCliente(this AppDb db, Guid tenantId, Guid id, CancellationToken ct = default) =>
        db.Clientes.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id && c.RemovidoEm == null, ct);

    public static async Task<Cliente> CriarCliente(this AppDb db, Guid tenantId, string nome, string? telefone, CancellationToken ct = default)
    {
        var c = new Cliente { TenantId = tenantId, Nome = nome, Telefone = telefone };
        db.Clientes.Add(c);
        await db.SaveChangesAsync(ct);
        db.Entry(c).State = EntityState.Detached; // o contexto não guarda o que gravou
        return c;
    }

    /// <summary>
    /// Agendamentos com inicio em [de, ate), com o nome do cliente, para a tela não
    /// fazer uma chamada por linha (1.6). prestadorId nulo = a empresa toda: quem
    /// monta o filtro é o escopo por prestador (2.7), nunca o handler.
    /// </summary>
    public static Task<List<AgendamentoComCliente>> ListarAgendamentos(this AppDb db, Guid tenantId,
        DateTime de, DateTime ate, Guid? prestadorId, CancellationToken ct = default) =>
        (from a in db.Agendamentos
         join c in db.Clientes
             on new { Id = a.ClienteId, a.TenantId } equals new { c.Id, c.TenantId }
         where a.TenantId == tenantId && a.Inicio >= de && a.Inicio < ate
             && (prestadorId == null || a.PrestadorId == prestadorId)
         orderby a.Inicio
         select new AgendamentoComCliente(a, c.Nome)).ToListAsync(ct);

    /// <summary>Nulo: inexistente ou de outro tenant.</summary>
    public static Task<Agendamento?> BuscarAgendamento(this AppDb db, Guid tenantId, Guid id, CancellationToken ct = default) =>
        db.Agendamentos.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == id, ct);

    /// <summary>
    /// Uma instrução só (1.5): valor_centavos copiado do serviço, nunca do payload;
    /// prestador só com membership ativo e serviço só ativo, senão zero linhas (devolve
    /// nulo). Sem fim, inicio + duração do serviço. Sobreposição fica com a constraint
    /// sem_sobreposicao: sai como PostgresException 23P01, porque checar antes no
    /// código abre condição de corrida.
    /// </summary>
    public static async Task<Agendamento?> CriarAgendamento(this AppDb db, Guid tenantId, NovoAgendamento n, CancellationToken ct = default)
    {
        var criados = await db.Agendamentos.FromSql($"""
            INSERT INTO agendamentos (tenant_id, cliente_id, servico_id, prestador_id, inicio, fim, valor_centavos)
            SELECT s.tenant_id, {n.ClienteId}, s.id, m.usuario_id, {n.Inicio},
                   COALESCE({n.Fim}::timestamptz, {n.Inicio} + s.duracao_minutos * interval '1 minute'),
                   s.preco_centavos
            FROM servicos s
            JOIN memberships m
              ON m.tenant_id = s.tenant_id AND m.usuario_id = {n.PrestadorId} AND m.removido_em IS NULL
            WHERE s.tenant_id = {tenantId} AND s.id = {n.ServicoId} AND s.ativo
            RETURNING *
            """).ToListAsync(ct);
        return criados.SingleOrDefault();
    }
}
