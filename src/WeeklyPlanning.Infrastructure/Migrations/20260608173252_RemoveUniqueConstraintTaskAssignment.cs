using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeeklyPlanning.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueConstraintTaskAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskAssignments_WeeklyPlanId_PersonId_ExternalTaskId",
                table: "TaskAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignments_WeeklyPlanId_PersonId",
                table: "TaskAssignments",
                columns: new[] { "WeeklyPlanId", "PersonId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskAssignments_WeeklyPlanId_PersonId",
                table: "TaskAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignments_WeeklyPlanId_PersonId_ExternalTaskId",
                table: "TaskAssignments",
                columns: new[] { "WeeklyPlanId", "PersonId", "ExternalTaskId" },
                unique: true);
        }
    }
}
