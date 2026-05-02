using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CHEF.Migrations.SpamIgnoreRoles
{
    public partial class Timeout : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "timeout_duration",
                table: "spam_filter_configs",
                type: "integer",
                nullable: false,
                defaultValue: 7);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "timeout_duration",
                table: "spam_filter_configs");
        }
    }
}
