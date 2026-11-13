using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibApp.Infrastructure.Db.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowRequestReservationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RoomBook_BorrowedCount_LE_BookCount",
                schema: "library",
                table: "RoomBook");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRequest_UserId_RoomBookId",
                schema: "library",
                table: "BorrowRequest");

            migrationBuilder.AddColumn<int>(
                name: "ReservedCount",
                schema: "library",
                table: "RoomBook",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                schema: "library",
                table: "BorrowRequest",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickedUpAt",
                schema: "library",
                table: "BorrowRequest",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoomBook_BorrowedPlusReserved_LE_BookCount",
                schema: "library",
                table: "RoomBook",
                sql: "(\"BorrowedCount\" + \"ReservedCount\") <= \"BookCount\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoomBook_ReservedCount_NonNegative",
                schema: "library",
                table: "RoomBook",
                sql: "\"ReservedCount\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRequest_ExpiresAt",
                schema: "library",
                table: "BorrowRequest",
                column: "ExpiresAt",
                filter: "\"Status\" = 'Approved'");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRequest_UserId_RoomBookId",
                schema: "library",
                table: "BorrowRequest",
                columns: new[] { "UserId", "RoomBookId" },
                unique: true,
                filter: "\"Status\" IN ('Pending', 'Approved')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RoomBook_BorrowedPlusReserved_LE_BookCount",
                schema: "library",
                table: "RoomBook");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RoomBook_ReservedCount_NonNegative",
                schema: "library",
                table: "RoomBook");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRequest_ExpiresAt",
                schema: "library",
                table: "BorrowRequest");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRequest_UserId_RoomBookId",
                schema: "library",
                table: "BorrowRequest");

            migrationBuilder.DropColumn(
                name: "ReservedCount",
                schema: "library",
                table: "RoomBook");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "library",
                table: "BorrowRequest");

            migrationBuilder.DropColumn(
                name: "PickedUpAt",
                schema: "library",
                table: "BorrowRequest");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoomBook_BorrowedCount_LE_BookCount",
                schema: "library",
                table: "RoomBook",
                sql: "\"BorrowedCount\" <= \"BookCount\"");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRequest_UserId_RoomBookId",
                schema: "library",
                table: "BorrowRequest",
                columns: new[] { "UserId", "RoomBookId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");
        }
    }
}
