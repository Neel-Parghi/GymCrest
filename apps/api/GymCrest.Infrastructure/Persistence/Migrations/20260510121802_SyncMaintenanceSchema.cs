using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncMaintenanceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename existing columns to match new entity property names
            migrationBuilder.RenameColumn(
                name: "TaskName",
                table: "MaintenanceLogs",
                newName: "ServiceType");

            migrationBuilder.RenameColumn(
                name: "AssignedTo",
                table: "MaintenanceLogs",
                newName: "TechnicianName");

            // Add truly missing columns
            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "MaintenanceLogs",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedEndDate",
                table: "MaintenanceLogs",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cost",
                table: "MaintenanceLogs");

            migrationBuilder.DropColumn(
                name: "EstimatedEndDate",
                table: "MaintenanceLogs");

            migrationBuilder.RenameColumn(
                name: "ServiceType",
                table: "MaintenanceLogs",
                newName: "TaskName");

            migrationBuilder.RenameColumn(
                name: "TechnicianName",
                table: "MaintenanceLogs",
                newName: "AssignedTo");
        }
    }
}
