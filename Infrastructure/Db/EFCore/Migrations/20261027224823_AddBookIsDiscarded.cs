using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibApp.Infrastructure.Db.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBookIsDiscarded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDiscarded",
                schema: "library",
                table: "Book",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Book_IsDiscarded",
                schema: "library",
                table: "Book",
                column: "IsDiscarded");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Book_IsDiscarded",
                schema: "library",
                table: "Book");

            migrationBuilder.DropColumn(
                name: "IsDiscarded",
                schema: "library",
                table: "Book");
        }
    }
}
