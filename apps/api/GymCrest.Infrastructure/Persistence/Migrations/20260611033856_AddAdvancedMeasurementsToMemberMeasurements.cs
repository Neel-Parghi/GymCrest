using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedMeasurementsToMemberMeasurements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Chest",
                table: "MemberMeasurements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Hips",
                table: "MemberMeasurements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdvanced",
                table: "MemberMeasurements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "LeftBicep",
                table: "MemberMeasurements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LeftCalf",
                table: "MemberMeasurements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LeftForearm",
                table: "MemberMeasurements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LeftThigh",
                table: "MemberMeasurements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowerAbs",
                table: "MemberMeasurements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Neck",
                table: "MemberMeasurements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RightBicep",
                table: "MemberMeasurements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RightCalf",
                table: "MemberMeasurements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RightForearm",
                table: "MemberMeasurements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RightThigh",
                table: "MemberMeasurements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Shoulders",
                table: "MemberMeasurements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "UpperAbs",
                table: "MemberMeasurements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Waist",
                table: "MemberMeasurements",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Chest",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "Hips",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "IsAdvanced",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "LeftBicep",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "LeftCalf",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "LeftForearm",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "LeftThigh",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "LowerAbs",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "Neck",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "RightBicep",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "RightCalf",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "RightForearm",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "RightThigh",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "Shoulders",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "UpperAbs",
                table: "MemberMeasurements");

            migrationBuilder.DropColumn(
                name: "Waist",
                table: "MemberMeasurements");
        }
    }
}
