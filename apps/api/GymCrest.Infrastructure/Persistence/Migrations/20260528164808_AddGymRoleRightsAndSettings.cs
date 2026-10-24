using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGymRoleRightsAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OperationsSettingsJson",
                table: "Gyms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleRightsMatrixJson",
                table: "Gyms",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperationsSettingsJson",
                table: "Gyms");

            migrationBuilder.DropColumn(
                name: "RoleRightsMatrixJson",
                table: "Gyms");
        }
    }
}
