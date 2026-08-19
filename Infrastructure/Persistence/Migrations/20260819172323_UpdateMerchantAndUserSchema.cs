using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMerchantAndUserSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MerchantId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AdminId",
                table: "Merchants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Merchants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Merchants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Merchants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_MerchantId",
                table: "Users",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_Email",
                table: "Merchants",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Merchants_MerchantId",
                table: "Users",
                column: "MerchantId",
                principalTable: "Merchants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Merchants_MerchantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_MerchantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Merchants_Email",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "MerchantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdminId",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Merchants");
        }
    }
}
