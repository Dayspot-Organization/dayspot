using Dayspot.Api.Dados;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dayspot.Api.Migrations;

[DbContext(typeof(AppDb))]
[Migration("20261124120000_RestringirAppUser")]
public sealed class RestringirAppUser : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- tenants, usuarios e memberships ficam fora do RLS, e o CASCADE de tenants apaga
            -- os dados da empresa inteira: a API nunca apaga nessas tabelas (remoção é
            -- removido_em). Mesma regra da 009 para agendamentos.
            REVOKE DELETE ON tenants, usuarios, memberships FROM app_user;

            -- Valem nas conexões novas. Lock preso ou consulta travada vira erro (500) em vez
            -- de segurar a conexão do pool até o cliente desistir.
            ALTER ROLE app_user SET lock_timeout = '5s';
            ALTER ROLE app_user SET statement_timeout = '15s';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER ROLE app_user RESET statement_timeout;
            ALTER ROLE app_user RESET lock_timeout;
            GRANT DELETE ON tenants, usuarios, memberships TO app_user;
            """);
    }
}
