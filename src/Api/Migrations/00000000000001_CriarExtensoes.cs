using Dayspot.Api.Dados;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dayspot.Api.Migrations;

[DbContext(typeof(AppDb))]
[Migration("00000000000001_CriarExtensoes")]
public sealed class CriarExtensoes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- btree_gist: a constraint de sobreposição de agendamentos (007) mistura = em uuid com && em intervalo.
            CREATE EXTENSION IF NOT EXISTS btree_gist;
            CREATE SCHEMA IF NOT EXISTS app;

            -- Tenant da transação, lido pelas policies de RLS. O `true` devolve NULL em vez de erro
            -- quando app.tenant_id não foi definido: sem tenant, nenhuma linha passa (falha fechada).
            CREATE FUNCTION app.current_tenant_id() RETURNS uuid
            LANGUAGE sql STABLE AS $$
              SELECT NULLIF(current_setting('app.tenant_id', true), '')::uuid
            $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP FUNCTION IF EXISTS app.current_tenant_id();
            DROP SCHEMA IF EXISTS app;
            DROP EXTENSION IF EXISTS btree_gist;
            """);
    }
}
