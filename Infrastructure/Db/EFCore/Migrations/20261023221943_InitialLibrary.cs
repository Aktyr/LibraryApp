using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibApp.Infrastructure.Db.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "library");

            migrationBuilder.CreateTable(
                name: "Book",
                schema: "library",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Author = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Publisher = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Genre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Book", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Room",
                schema: "library",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Room", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                schema: "library",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContactInfo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscardedBooks",
                schema: "library",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    DiscardedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DiscardReason = table.Column<string>(type: "text", nullable: false),
                    ApprovedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CompensationAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscardedBooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscardedBooks_Book_BookId",
                        column: x => x.BookId,
                        principalSchema: "library",
                        principalTable: "Book",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscardedBooks_Room_RoomId",
                        column: x => x.RoomId,
                        principalSchema: "library",
                        principalTable: "Room",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomBook",
                schema: "library",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookCount = table.Column<int>(type: "integer", nullable: false),
                    BorrowedCount = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomBook", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomBook_Book_BookId",
                        column: x => x.BookId,
                        principalSchema: "library",
                        principalTable: "Book",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomBook_Room_RoomId",
                        column: x => x.RoomId,
                        principalSchema: "library",
                        principalTable: "Room",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoomBook",
                schema: "library",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BorrowDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReturnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Penalty = table.Column<decimal>(type: "numeric", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomBookId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoomBook", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoomBook_RoomBook_RoomBookId",
                        column: x => x.RoomBookId,
                        principalSchema: "library",
                        principalTable: "RoomBook",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoomBook_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "library",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Book_Author",
                schema: "library",
                table: "Book",
                column: "Author");

            migrationBuilder.CreateIndex(
                name: "IX_Book_Title",
                schema: "library",
                table: "Book",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_DiscardedBooks_BookId",
                schema: "library",
                table: "DiscardedBooks",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscardedBooks_RoomId",
                schema: "library",
                table: "DiscardedBooks",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Room_Name",
                schema: "library",
                table: "Room",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomBook_BookId",
                schema: "library",
                table: "RoomBook",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomBook_RoomId",
                schema: "library",
                table: "RoomBook",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                schema: "library",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoomBook_BorrowDate",
                schema: "library",
                table: "UserRoomBook",
                column: "BorrowDate");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoomBook_Deadline",
                schema: "library",
                table: "UserRoomBook",
                column: "Deadline");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoomBook_RoomBookId",
                schema: "library",
                table: "UserRoomBook",
                column: "RoomBookId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoomBook_UserId",
                schema: "library",
                table: "UserRoomBook",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscardedBooks",
                schema: "library");

            migrationBuilder.DropTable(
                name: "UserRoomBook",
                schema: "library");

            migrationBuilder.DropTable(
                name: "RoomBook",
                schema: "library");

            migrationBuilder.DropTable(
                name: "User",
                schema: "library");

            migrationBuilder.DropTable(
                name: "Book",
                schema: "library");

            migrationBuilder.DropTable(
                name: "Room",
                schema: "library");
        }
    }
}
