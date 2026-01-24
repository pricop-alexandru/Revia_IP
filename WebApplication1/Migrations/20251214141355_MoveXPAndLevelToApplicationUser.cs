using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revia.Migrations
{
    /// <inheritdoc />
    public partial class MoveXPAndLevelToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Level",
                table: "LocalGuides");

            migrationBuilder.DropColumn(
                name: "XP",
                table: "LocalGuides");

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "XP",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Level",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "XP",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "LocalGuides",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "XP",
                table: "LocalGuides",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
