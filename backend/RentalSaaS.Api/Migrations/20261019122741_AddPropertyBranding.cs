using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalSaaS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccentColor",
                table: "Properties",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LogoDataUrl",
                table: "Properties",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "Properties",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccentColor",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "LogoDataUrl",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "Properties");
        }
    }
}
