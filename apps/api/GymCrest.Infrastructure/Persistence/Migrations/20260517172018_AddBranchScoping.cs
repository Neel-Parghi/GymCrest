using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "Staff",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "InventoryItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "GymMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staff_BranchId",
                table: "Staff",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_BranchId",
                table: "InventoryItems",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_GymMembers_BranchId",
                table: "GymMembers",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_GymMembers_Branches_BranchId",
                table: "GymMembers",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Branches_BranchId",
                table: "InventoryItems",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Staff_Branches_BranchId",
                table: "Staff",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GymMembers_Branches_BranchId",
                table: "GymMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Branches_BranchId",
                table: "InventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Staff_Branches_BranchId",
                table: "Staff");

            migrationBuilder.DropIndex(
                name: "IX_Staff_BranchId",
                table: "Staff");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_BranchId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_GymMembers_BranchId",
                table: "GymMembers");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "GymMembers");
        }
    }
}
