using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodSearchSwitch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFoodSearchEnabled",
                table: "SaaSConfigurations",
                type: "boolean",
                nullable: false,
                // On by default: food search keeps working for every existing install
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "SaaSConfigurations",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"),
                column: "IsFoodSearchEnabled",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFoodSearchEnabled",
                table: "SaaSConfigurations");
        }
    }
}
