using Dayspot.Api.Dados;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dayspot.Api.Migrations;

[DbContext(typeof(AppDb))]
[Migration("20261127003555_CriarConvites")]
public sealed class CriarConvites : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- Fora do RLS (guia §009): o aceite acha o convite pelo token, antes de existir
            -- tenant. Listar e criar levam WHERE tenant_id manual.
            CREATE TABLE convites (
              id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
              tenant_id  uuid NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
              email      text NOT NULL,
              papel      text NOT NULL,
              token      uuid NOT NULL DEFAULT gen_random_uuid(),
              expira_em  timestamptz NOT NULL DEFAULT now() + interval '7 days',
              aceito_em  timestamptz,
              criado_por uuid NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
              criado_em  timestamptz NOT NULL DEFAULT now(),
              CONSTRAINT convite_papel_valido CHECK (papel IN ('administrador','prestador')),
              CONSTRAINT convite_token_unico  UNIQUE (token)
            );

            CREATE INDEX idx_convites_tenant ON convites (tenant_id);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE convites;
            """);
    }
}
