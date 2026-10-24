using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InsertGymMembersTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AddressId",
                table: "GymMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BloodGroup",
                table: "GymMembers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "FitnessGoals",
                table: "GymMembers",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalConditions",
                table: "GymMembers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymMembers_AddressId",
                table: "GymMembers",
                column: "AddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_GymMembers_Addresses_AddressId",
                table: "GymMembers",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GymMembers_Addresses_AddressId",
                table: "GymMembers");

            migrationBuilder.DropIndex(
                name: "IX_GymMembers_AddressId",
                table: "GymMembers");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "GymMembers");

            migrationBuilder.DropColumn(
                name: "BloodGroup",
                table: "GymMembers");

            migrationBuilder.DropColumn(
                name: "FitnessGoals",
                table: "GymMembers");

            migrationBuilder.DropColumn(
                name: "MedicalConditions",
                table: "GymMembers");
        }
    }
}
