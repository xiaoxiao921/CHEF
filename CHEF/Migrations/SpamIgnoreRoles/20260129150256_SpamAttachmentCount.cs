using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CHEF.Migrations.SpamIgnoreRoles
{
    public partial class SpamAttachmentCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attachment_count_for_spam",
                table: "spam_filter_configs",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<int>(
                name: "embed_count_for_spam",
                table: "spam_filter_configs",
                type: "integer",
                nullable: false,
                defaultValue: 4);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attachment_count_for_spam",
                table: "spam_filter_configs");

            migrationBuilder.DropColumn(
                name: "embed_count_for_spam",
                table: "spam_filter_configs");
        }
    }
}
