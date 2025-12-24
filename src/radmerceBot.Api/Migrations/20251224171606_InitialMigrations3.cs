using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace radmerceBot.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrations3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TelId",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelId",
                table: "Users");
        }
    }
}
