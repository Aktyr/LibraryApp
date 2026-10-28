using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibApp.Infrastructure.Db.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIsDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_Email",
                schema: "library",
                table: "User");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "library",
                table: "User",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "library",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedById",
                schema: "library",
                table: "DiscardedBooks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                schema: "library",
                table: "User",
                column: "Email",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_User_IsDeleted",
                schema: "library",
                table: "User",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DiscardedBooks_ApprovedById",
                schema: "library",
                table: "DiscardedBooks",
                column: "ApprovedById");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscardedBooks_User_ApprovedById",
                schema: "library",
                table: "DiscardedBooks",
                column: "ApprovedById",
                principalSchema: "library",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscardedBooks_User_ApprovedById",
                schema: "library",
                table: "DiscardedBooks");

            migrationBuilder.DropIndex(
                name: "IX_User_Email",
                schema: "library",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_IsDeleted",
                schema: "library",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_DiscardedBooks_ApprovedById",
                schema: "library",
                table: "DiscardedBooks");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "library",
                table: "User");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "library",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                schema: "library",
                table: "DiscardedBooks");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                schema: "library",
                table: "User",
                column: "Email",
                unique: true);
        }
    }
}
