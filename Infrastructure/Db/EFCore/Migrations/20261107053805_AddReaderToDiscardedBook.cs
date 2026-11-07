using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibApp.Infrastructure.Db.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddReaderToDiscardedBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReaderId",
                schema: "library",
                table: "DiscardedBooks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReaderName",
                schema: "library",
                table: "DiscardedBooks",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscardedBooks_ReaderId",
                schema: "library",
                table: "DiscardedBooks",
                column: "ReaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscardedBooks_User_ReaderId",
                schema: "library",
                table: "DiscardedBooks",
                column: "ReaderId",
                principalSchema: "library",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscardedBooks_User_ReaderId",
                schema: "library",
                table: "DiscardedBooks");

            migrationBuilder.DropIndex(
                name: "IX_DiscardedBooks_ReaderId",
                schema: "library",
                table: "DiscardedBooks");

            migrationBuilder.DropColumn(
                name: "ReaderId",
                schema: "library",
                table: "DiscardedBooks");

            migrationBuilder.DropColumn(
                name: "ReaderName",
                schema: "library",
                table: "DiscardedBooks");
        }
    }
}
