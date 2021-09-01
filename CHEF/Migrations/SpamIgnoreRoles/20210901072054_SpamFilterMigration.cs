using Microsoft.EntityFrameworkCore.Migrations;

namespace CHEF.Migrations.SpamIgnoreRoles
{
    public partial class SpamFilterMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "spam_ignore_roles",
                columns: table => new
                {
                    discord_id = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_spam_ignore_roles", x => x.discord_id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "spam_ignore_roles");
        }
    }
}
