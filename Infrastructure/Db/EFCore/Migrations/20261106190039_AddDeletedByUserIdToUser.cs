using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibApp.Infrastructure.Db.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedByUserIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                schema: "library",
                table: "User",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_DeletedByUserId",
                schema: "library",
                table: "User",
                column: "DeletedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_User_User_DeletedByUserId",
                schema: "library",
                table: "User",
                column: "DeletedByUserId",
                principalSchema: "library",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_User_DeletedByUserId",
                schema: "library",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_DeletedByUserId",
                schema: "library",
                table: "User");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "library",
                table: "User");
        }
    }
}
