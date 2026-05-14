using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeeklyPlanning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonalWeeklyPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WeekStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WeekEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalWeeklyPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonalPlanItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonalWeeklyPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EstimatedHours = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    PlannedDayOfWeek = table.Column<int>(type: "int", nullable: true),
                    ExternalTaskId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsDone = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalPlanItems_PersonalWeeklyPlans_PersonalWeeklyPlanId",
                        column: x => x.PersonalWeeklyPlanId,
                        principalTable: "PersonalWeeklyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalPlanItems_PersonalWeeklyPlanId",
                table: "PersonalPlanItems",
                column: "PersonalWeeklyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalWeeklyPlans_OwnerId_WeekStartDate",
                table: "PersonalWeeklyPlans",
                columns: new[] { "OwnerId", "WeekStartDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonalPlanItems");

            migrationBuilder.DropTable(
                name: "PersonalWeeklyPlans");
        }
    }
}
