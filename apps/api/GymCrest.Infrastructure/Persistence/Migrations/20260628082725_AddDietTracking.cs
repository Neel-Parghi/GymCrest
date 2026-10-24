using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDietTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Protein",
                table: "DietPlanMeals",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.CreateTable(
                name: "DietLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    LogDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TargetCalories = table.Column<int>(type: "integer", nullable: false),
                    TargetProtein = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TargetCarbs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TargetFats = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalCalories = table.Column<int>(type: "integer", nullable: false),
                    TotalProtein = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalCarbs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalFats = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DietLogs_Users_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DietLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    MealType = table.Column<string>(type: "text", nullable: false),
                    FoodName = table.Column<string>(type: "text", nullable: false),
                    Calories = table.Column<int>(type: "integer", nullable: false),
                    Protein = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Carbs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Fats = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SourceDietPlanMealId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalFoodId = table.Column<string>(type: "text", nullable: true),
                    ServingSize = table.Column<decimal>(type: "numeric", nullable: false),
                    ServingUnit = table.Column<string>(type: "text", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealLogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealLogEntries_DietLogs_DietLogId",
                        column: x => x.DietLogId,
                        principalTable: "DietLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DietLogs_MemberId",
                table: "DietLogs",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MealLogEntries_DietLogId",
                table: "MealLogEntries",
                column: "DietLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MealLogEntries");

            migrationBuilder.DropTable(
                name: "DietLogs");

            migrationBuilder.AlterColumn<decimal>(
                name: "Protein",
                table: "DietPlanMeals",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }
    }
}
