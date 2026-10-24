using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SupportStandaloneUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberMeasurements_GymMembers_MemberId",
                table: "MemberMeasurements");

            migrationBuilder.AlterColumn<Guid>(
                name: "MemberId",
                table: "WorkoutSessionLogs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "WorkoutSessionLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GymId",
                table: "WorkoutPlans",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "MemberId",
                table: "MemberPlanAssignments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "MemberPlanAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "MemberId",
                table: "MemberMeasurements",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "MemberMeasurements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "MemberId",
                table: "MemberDietAssignments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "MemberDietAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GymId",
                table: "DietPlans",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessionLogs_UserId",
                table: "WorkoutSessionLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberPlanAssignments_UserId",
                table: "MemberPlanAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberMeasurements_UserId",
                table: "MemberMeasurements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberDietAssignments_UserId",
                table: "MemberDietAssignments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberDietAssignments_Users_UserId",
                table: "MemberDietAssignments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemberMeasurements_GymMembers_MemberId",
                table: "MemberMeasurements",
                column: "MemberId",
                principalTable: "GymMembers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberMeasurements_Users_UserId",
                table: "MemberMeasurements",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemberPlanAssignments_Users_UserId",
                table: "MemberPlanAssignments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessionLogs_Users_UserId",
                table: "WorkoutSessionLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberDietAssignments_Users_UserId",
                table: "MemberDietAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_MemberMeasurements_GymMembers_MemberId",
                table: "MemberMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_MemberMeasurements_Users_UserId",
                table: "MemberMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_MemberPlanAssignments_Users_UserId",
                table: "MemberPlanAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessionLogs_Users_UserId",
                table: "WorkoutSessionLogs");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessionLogs_UserId",
                table: "WorkoutSessionLogs");

            migrationBuilder.DropIndex(
                name: "IX_MemberPlanAssignments_UserId",
                table: "MemberPlanAssignments");

            migrationBuilder.DropIndex(
                name: "IX_MemberMeasurements_UserId",
                table: "MemberMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_MemberDietAssignments_UserId",
                table: "MemberDietAssignments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "WorkoutSessionLogs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "MemberPlanAssignments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "MemberDietAssignments");

            migrationBuilder.AlterColumn<Guid>(
                name: "MemberId",
                table: "WorkoutSessionLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GymId",
                table: "WorkoutPlans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "MemberId",
                table: "MemberPlanAssignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "MemberId",
                table: "MemberMeasurements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "MemberId",
                table: "MemberDietAssignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GymId",
                table: "DietPlans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MemberMeasurements_GymMembers_MemberId",
                table: "MemberMeasurements",
                column: "MemberId",
                principalTable: "GymMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
