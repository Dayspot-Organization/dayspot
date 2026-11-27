using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;

namespace Dayspot.Api.Dados;

/// <summary>
/// Única porta para o banco (requisitos §6.2, camada b). O schema vem das EF Core
/// Migrations em SQL (src/Api/Migrations); o modelo aqui só o mapeia.
/// </summary>
public sealed class AppDb(DbContextOptions<AppDb> opcoes) : DbContext(opcoes)
{
    // O nome do DbSet, em snake_case, é o da tabela.
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Disponibilidade> Disponibilidades => Set<Disponibilidade>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    public DbSet<Sessao> Sessoes => Set<Sessao>();
    public DbSet<Convite> Convites => Set<Convite>();

    /// <summary>
    /// Opções do contexto sobre a connection string do app_user (DATABASE_URL):
    /// nomes em snake_case, como no schema, e leitura sem rastreamento.
    /// </summary>
    public static DbContextOptions<AppDb> Opcoes(string connectionString) =>
        new DbContextOptionsBuilder<AppDb>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        // Colunas com DEFAULT no banco: o INSERT não as envia e o EF lê o valor de volta.
        modelo.Entity<Cliente>(e =>
        {
            e.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(c => c.VinculoExpiraEm).HasDefaultValueSql("now() + interval '30 days'");
            e.Property(c => c.CriadoEm).HasDefaultValueSql("now()");
        });
    }

    /// <summary>
    /// Abre uma transação com set_config('app.tenant_id', ..., true): o tenant morre
    /// com a transação e não vaza para quem pegar a conexão depois (guia, armadilha
    /// 2). Sem CommitAsync, o Dispose desfaz.
    /// </summary>
    public async Task<IDbContextTransaction> IniciarComTenant(Guid tenantId, CancellationToken ct = default)
    {
        var tx = await Database.BeginTransactionAsync(ct);
        try
        {
            await Database.ExecuteSqlInterpolatedAsync(
                $"SELECT set_config('app.tenant_id', {tenantId.ToString()}, true)", ct);
            return tx;
        }
        catch
        {
            await tx.DisposeAsync();
            throw;
        }
    }
}

/// <summary>
/// Contexto do dotnet ef (migrations), sobre a conexão do dono das tabelas
/// (MIGRATIONS_DATABASE_URL). A API não passa por aqui. O dotnet ef roda em
/// src/Api, e o .env fica na raiz do repositório.
/// </summary>
public sealed class AppDbDesignTime : IDesignTimeDbContextFactory<AppDb>
{
    public AppDb CreateDbContext(string[] args) =>
        new(AppDb.Opcoes(Config.MigrationsConnectionString(Path.Combine("..", "..", ".env"))));
}
