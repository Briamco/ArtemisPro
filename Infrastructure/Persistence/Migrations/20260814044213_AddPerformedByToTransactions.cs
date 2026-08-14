using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformedByToTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PerformedById",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PerformedById",
                table: "Transactions",
                column: "PerformedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_PerformedById",
                table: "Transactions",
                column: "PerformedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_PerformedById",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PerformedById",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PerformedById",
                table: "Transactions");
        }
    }
}
