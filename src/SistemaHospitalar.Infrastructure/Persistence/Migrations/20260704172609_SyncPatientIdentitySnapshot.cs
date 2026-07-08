using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations;

/// <summary>
/// Sincroniza o snapshot do modelo com o código. Sem alterações no banco — tabelas já existem via migrations anteriores.
/// </summary>
public partial class SyncPatientIdentitySnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
