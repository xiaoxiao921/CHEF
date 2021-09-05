using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CHEF.Migrations.SpamIgnoreRoles
{
    public partial class SpamFilterConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "spam_filter_configs",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    action_on_spam = table.Column<int>(nullable: false),
                    mute_role_id = table.Column<decimal>(nullable: false),
                    messages_for_action = table.Column<int>(nullable: false),
                    message_seconds_gap = table.Column<int>(nullable: false),
                    inlcude_message_content_in_log = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_spam_filter_configs", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "spam_filter_configs");
        }
    }
}
