using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibApp.Infrastructure.Db.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBookDescriptionAndCoverUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverUrl",
                schema: "library",
                table: "Book",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "library",
                table: "Book",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverUrl",
                schema: "library",
                table: "Book");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "library",
                table: "Book");
        }
    }
}
