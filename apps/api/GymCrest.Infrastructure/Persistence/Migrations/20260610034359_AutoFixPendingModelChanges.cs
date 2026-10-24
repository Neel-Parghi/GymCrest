using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AutoFixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Skipped",
                table: "LoggedExercises",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Skipped",
                table: "LoggedExercises");
        }
    }
}
