using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeeklyPlanning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSentToExternalToAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalCreatedTaskId",
                table: "TaskAssignments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentToExternalAt",
                table: "TaskAssignments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalCreatedTaskId",
                table: "TaskAssignments");

            migrationBuilder.DropColumn(
                name: "SentToExternalAt",
                table: "TaskAssignments");
        }
    }
}
