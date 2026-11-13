using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibApp.Infrastructure.Db.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BorrowRequestId",
                schema: "library",
                table: "UserRoomBook",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BorrowRequest",
                schema: "library",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomBookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BorrowRequest_RoomBook_RoomBookId",
                        column: x => x.RoomBookId,
                        principalSchema: "library",
                        principalTable: "RoomBook",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BorrowRequest_User_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalSchema: "library",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BorrowRequest_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "library",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoomBook_BorrowRequestId",
                schema: "library",
                table: "UserRoomBook",
                column: "BorrowRequestId",
                unique: true,
                filter: "\"BorrowRequestId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRequest_ProcessedByUserId",
                schema: "library",
                table: "BorrowRequest",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRequest_RoomBookId",
                schema: "library",
                table: "BorrowRequest",
                column: "RoomBookId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRequest_Status",
                schema: "library",
                table: "BorrowRequest",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRequest_UserId",
                schema: "library",
                table: "BorrowRequest",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRequest_UserId_RoomBookId",
                schema: "library",
                table: "BorrowRequest",
                columns: new[] { "UserId", "RoomBookId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoomBook_BorrowRequest_BorrowRequestId",
                schema: "library",
                table: "UserRoomBook",
                column: "BorrowRequestId",
                principalSchema: "library",
                principalTable: "BorrowRequest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoomBook_BorrowRequest_BorrowRequestId",
                schema: "library",
                table: "UserRoomBook");

            migrationBuilder.DropTable(
                name: "BorrowRequest",
                schema: "library");

            migrationBuilder.DropIndex(
                name: "IX_UserRoomBook_BorrowRequestId",
                schema: "library",
                table: "UserRoomBook");

            migrationBuilder.DropColumn(
                name: "BorrowRequestId",
                schema: "library",
                table: "UserRoomBook");
        }
    }
}
