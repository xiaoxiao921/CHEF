using Microsoft.EntityFrameworkCore.Migrations;

namespace CHEF.Migrations.SpamIgnoreRoles
{
    public partial class FixTypo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn("inlcude_message_content_in_log", "spam_filter_configs", "include_message_content_in_log");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn("include_message_content_in_log", "spam_filter_configs", "inlcude_message_content_in_log");
        }
    }
}
