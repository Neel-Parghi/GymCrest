using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberFreezeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FreezeStartDate",
                table: "GymMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FreezeUntil",
                table: "GymMembers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FreezeStartDate",
                table: "GymMembers");

            migrationBuilder.DropColumn(
                name: "FreezeUntil",
                table: "GymMembers");
        }
    }
}
