using Dayspot.Api.Dados;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dayspot.Api.Migrations;

[DbContext(typeof(AppDb))]
[Migration("00000000000010_CriarSessoes")]
public sealed class CriarSessoes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- Fora do RLS (guia §009): a sessão é lida pelo cookie justamente para descobrir o
            -- tenant. Toda consulta aqui leva WHERE manual.
            CREATE TABLE sessoes (
              id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
              usuario_id  uuid NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
              -- Nulo até a empresa ser escolhida; nunca vem do corpo da requisição.
              tenant_id   uuid REFERENCES tenants(id) ON DELETE CASCADE,
              criado_em   timestamptz NOT NULL DEFAULT now(),
              expira_em   timestamptz NOT NULL,
              -- Logout grava aqui (2.4): sessão revogada não volta a valer.
              revogado_em timestamptz,
              user_agent  text,
              CONSTRAINT expira_depois_de_criar CHECK (expira_em > criado_em)
            );

            CREATE INDEX idx_sessoes_usuario ON sessoes (usuario_id);
            CREATE INDEX idx_sessoes_validas ON sessoes (expira_em) WHERE revogado_em IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE sessoes;
            """);
    }
}
