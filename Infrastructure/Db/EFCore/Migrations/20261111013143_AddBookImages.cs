using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibApp.Infrastructure.Db.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBookImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookImage",
                schema: "library",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsMain = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookImage_Book_BookId",
                        column: x => x.BookId,
                        principalSchema: "library",
                        principalTable: "Book",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO library."BookImage" ("Id", "BookId", "Url", "SortOrder", "IsMain")
                SELECT gen_random_uuid(), "Id", "CoverUrl", 0, true
                FROM library."Book"
                WHERE "CoverUrl" IS NOT NULL AND "CoverUrl" <> '';
                """);

            migrationBuilder.DropColumn(
                name: "CoverUrl",
                schema: "library",
                table: "Book");

            migrationBuilder.CreateIndex(
                name: "IX_BookImage_BookId",
                schema: "library",
                table: "BookImage",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BookImage_BookId_SortOrder",
                schema: "library",
                table: "BookImage",
                columns: new[] { "BookId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookImage",
                schema: "library");

            migrationBuilder.AddColumn<string>(
                name: "CoverUrl",
                schema: "library",
                table: "Book",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
