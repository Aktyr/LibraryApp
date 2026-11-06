using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibApp.Infrastructure.Db.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPenaltyPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PenaltyPayment",
                schema: "library",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserRoomBookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PenaltyPayment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PenaltyPayment_UserRoomBook_UserRoomBookId",
                        column: x => x.UserRoomBookId,
                        principalSchema: "library",
                        principalTable: "UserRoomBook",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PenaltyPayment_User_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalSchema: "library",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PenaltyPayment_PaidAt",
                schema: "library",
                table: "PenaltyPayment",
                column: "PaidAt");

            migrationBuilder.CreateIndex(
                name: "IX_PenaltyPayment_PaidByUserId",
                schema: "library",
                table: "PenaltyPayment",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PenaltyPayment_UserRoomBookId",
                schema: "library",
                table: "PenaltyPayment",
                column: "UserRoomBookId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PenaltyPayment",
                schema: "library");
        }
    }
}
