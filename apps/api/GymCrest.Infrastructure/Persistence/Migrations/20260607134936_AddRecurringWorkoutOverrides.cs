using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringWorkoutOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberWorkoutScheduleDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberPlanAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<string>(type: "text", nullable: false),
                    WorkoutPlanDayId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsRestDay = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberWorkoutScheduleDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberWorkoutScheduleDays_MemberPlanAssignments_MemberPlanA~",
                        column: x => x.MemberPlanAssignmentId,
                        principalTable: "MemberPlanAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberWorkoutScheduleDays_WorkoutPlanDays_WorkoutPlanDayId",
                        column: x => x.WorkoutPlanDayId,
                        principalTable: "WorkoutPlanDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberWorkoutScheduleDays_MemberPlanAssignmentId_DayOfWeek",
                table: "MemberWorkoutScheduleDays",
                columns: new[] { "MemberPlanAssignmentId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberWorkoutScheduleDays_WorkoutPlanDayId",
                table: "MemberWorkoutScheduleDays",
                column: "WorkoutPlanDayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberWorkoutScheduleDays");
        }
    }
}
