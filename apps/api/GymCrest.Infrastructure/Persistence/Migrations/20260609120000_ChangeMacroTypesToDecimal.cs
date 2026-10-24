using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    public partial class ChangeMacroTypesToDecimal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alter DietPlans macro columns to decimal (numeric)
            migrationBuilder.AlterColumn<decimal>(
                name: "Protein",
                table: "DietPlans",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Carbs",
                table: "DietPlans",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Fats",
                table: "DietPlans",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            // Alter DietPlanMeals macro column to decimal
            migrationBuilder.AlterColumn<decimal>(
                name: "Protein",
                table: "DietPlanMeals",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert DietPlans macro columns back to int
            migrationBuilder.AlterColumn<int>(
                name: "Protein",
                table: "DietPlans",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "Carbs",
                table: "DietPlans",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "Fats",
                table: "DietPlans",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            // Revert DietPlanMeals macro column back to int
            migrationBuilder.AlterColumn<int>(
                name: "Protein",
                table: "DietPlanMeals",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }
    }
}
