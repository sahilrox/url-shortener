using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryToClickEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "ClickEvents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "ClickEvents");
        }
    }
}
