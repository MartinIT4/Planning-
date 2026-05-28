using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeeklyPlanning.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerIdToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyPlans_WeekStartDate",
                table: "WeeklyPlans");

            migrationBuilder.DropIndex(
                name: "IX_Persons_Email",
                table: "Persons");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "WeeklyPlans",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Projects",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Persons",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE \"WeeklyPlans\" SET \"OwnerId\" = 'martin.ramirez@it4w.net' WHERE \"OwnerId\" = ''; ");
            migrationBuilder.Sql("UPDATE \"Projects\" SET \"OwnerId\" = 'martin.ramirez@it4w.net' WHERE \"OwnerId\" = ''; ");
            migrationBuilder.Sql("UPDATE \"Persons\" SET \"OwnerId\" = 'martin.ramirez@it4w.net' WHERE \"OwnerId\" = ''; ");
            migrationBuilder.Sql("ALTER TABLE \"WeeklyPlans\" ALTER COLUMN \"OwnerId\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"Projects\" ALTER COLUMN \"OwnerId\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"Persons\" ALTER COLUMN \"OwnerId\" DROP DEFAULT;");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyPlans_OwnerId_WeekStartDate",
                table: "WeeklyPlans",
                columns: new[] { "OwnerId", "WeekStartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Persons_OwnerId_Email",
                table: "Persons",
                columns: new[] { "OwnerId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyPlans_OwnerId_WeekStartDate",
                table: "WeeklyPlans");

            migrationBuilder.DropIndex(
                name: "IX_Persons_OwnerId_Email",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "WeeklyPlans");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Persons");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyPlans_WeekStartDate",
                table: "WeeklyPlans",
                column: "WeekStartDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Email",
                table: "Persons",
                column: "Email",
                unique: true);
        }
    }
}
