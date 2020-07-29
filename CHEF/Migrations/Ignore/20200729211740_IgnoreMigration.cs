using Microsoft.EntityFrameworkCore.Migrations;

namespace CHEF.Migrations.Ignore
{
    public partial class IgnoreMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_ids",
                columns: table => new
                {
                    discord_id = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_ids", x => x.discord_id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_ids");
        }
    }
}
