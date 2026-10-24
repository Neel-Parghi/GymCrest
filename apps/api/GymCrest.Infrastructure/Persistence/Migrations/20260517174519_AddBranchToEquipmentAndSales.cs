using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchToEquipmentAndSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "SaleTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "Equipment",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleTransactions_BranchId",
                table: "SaleTransactions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_BranchId",
                table: "Equipment",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_Branches_BranchId",
                table: "Equipment",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleTransactions_Branches_BranchId",
                table: "SaleTransactions",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_Branches_BranchId",
                table: "Equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleTransactions_Branches_BranchId",
                table: "SaleTransactions");

            migrationBuilder.DropIndex(
                name: "IX_SaleTransactions_BranchId",
                table: "SaleTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_BranchId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SaleTransactions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Equipment");
        }
    }
}
