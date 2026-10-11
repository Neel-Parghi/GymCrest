using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberPreferencesAndDietType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActivityLevel",
                table: "UserPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DietPreference",
                table: "UserPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Limitations",
                table: "UserPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowGymBuddy",
                table: "UserPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "TrainingDaysPerWeek",
                table: "UserPreferences",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DietType",
                table: "DietPlans",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivityLevel",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "DietPreference",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "Limitations",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ShowGymBuddy",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "TrainingDaysPerWeek",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "DietType",
                table: "DietPlans");
        }
    }
}
