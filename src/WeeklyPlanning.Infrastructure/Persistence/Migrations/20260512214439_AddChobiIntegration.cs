using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeeklyPlanning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChobiIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChobiProjectId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChobiUserId",
                table: "Persons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChobiProjectId",
                table: "PersonalPlanItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalTaskUrl",
                table: "PersonalPlanItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChobiProjectId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ChobiUserId",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "ChobiProjectId",
                table: "PersonalPlanItems");

            migrationBuilder.DropColumn(
                name: "ExternalTaskUrl",
                table: "PersonalPlanItems");
        }
    }
}
