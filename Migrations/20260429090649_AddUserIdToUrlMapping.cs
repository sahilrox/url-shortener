using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToUrlMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Urls",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Urls");
        }
    }
}
