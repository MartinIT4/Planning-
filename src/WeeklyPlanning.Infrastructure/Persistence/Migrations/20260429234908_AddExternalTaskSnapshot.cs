using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeeklyPlanning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalTaskSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalTaskSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalTaskId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProjectId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(7,2)", nullable: false),
                    LoggedHours = table.Column<decimal>(type: "decimal(7,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssigneeExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssigneeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalTaskSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTaskSnapshots_ExternalTaskId",
                table: "ExternalTaskSnapshots",
                column: "ExternalTaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTaskSnapshots_IsActive",
                table: "ExternalTaskSnapshots",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTaskSnapshots_LastSyncedAt",
                table: "ExternalTaskSnapshots",
                column: "LastSyncedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalTaskSnapshots");
        }
    }
}
