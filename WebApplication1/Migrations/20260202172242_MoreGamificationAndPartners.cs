using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revia.Migrations
{
    /// <inheritdoc />
    public partial class MoreGamificationAndPartners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOfficialPartner",
                table: "Locations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RequiredLevel",
                table: "Coupons",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOfficialPartner",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "RequiredLevel",
                table: "Coupons");
        }
    }
}
