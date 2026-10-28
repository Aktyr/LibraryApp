using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibApp.Infrastructure.Db.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoomBookUserRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoomBook_User_UserId",
                schema: "library",
                table: "UserRoomBook");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoomBook_User_UserId",
                schema: "library",
                table: "UserRoomBook",
                column: "UserId",
                principalSchema: "library",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoomBook_User_UserId",
                schema: "library",
                table: "UserRoomBook");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoomBook_User_UserId",
                schema: "library",
                table: "UserRoomBook",
                column: "UserId",
                principalSchema: "library",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
