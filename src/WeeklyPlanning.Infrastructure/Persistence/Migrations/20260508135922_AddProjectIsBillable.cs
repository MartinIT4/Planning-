using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeeklyPlanning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectIsBillable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBillable",
                table: "Projects",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBillable",
                table: "Projects");
        }
    }
}
