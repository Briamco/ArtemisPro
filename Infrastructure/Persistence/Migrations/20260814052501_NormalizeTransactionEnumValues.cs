using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeTransactionEnumValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Transactions SET Type = 'DÉBITO' WHERE Type = 'Debito'");
            migrationBuilder.Sql("UPDATE Transactions SET Type = 'CRÉDITO' WHERE Type = 'Credito'");
            migrationBuilder.Sql("UPDATE Transactions SET Status = 'APROBADA' WHERE Status = 'Aprobada'");
            migrationBuilder.Sql("UPDATE Transactions SET Status = 'RECHAZADA' WHERE Status = 'Rechazada'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Transactions SET Type = 'Debito' WHERE Type = 'DÉBITO'");
            migrationBuilder.Sql("UPDATE Transactions SET Type = 'Credito' WHERE Type = 'CRÉDITO'");
            migrationBuilder.Sql("UPDATE Transactions SET Status = 'Aprobada' WHERE Status = 'APROBADA'");
            migrationBuilder.Sql("UPDATE Transactions SET Status = 'Rechazada' WHERE Status = 'RECHAZADA'");
        }
    }
}
