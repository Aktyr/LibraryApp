using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibApp.Infrastructure.Db.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class SyncEntityLimitsAndConstraintsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoomBook_RoomBook_RoomBookId",
                schema: "library",
                table: "UserRoomBook");

            migrationBuilder.AlterColumn<decimal>(
                name: "Penalty",
                schema: "library",
                table: "UserRoomBook",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            // EF не генерирует USING для int→text. Без CASE PostgreSQL приведёт int к '0'/'1'/'2',
            // а UserRoleConverter ожидает 'Reader'/'Librarian'/'Admin' — приложение упадёт на чтении.
            migrationBuilder.Sql("""
                ALTER TABLE library."User"
                ALTER COLUMN "Role" TYPE text
                USING CASE "Role"::int
                    WHEN 0 THEN 'Reader'
                    WHEN 1 THEN 'Librarian'
                    WHEN 2 THEN 'Admin'
                    ELSE 'Reader'
                END;
                """);
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                schema: "library",
                table: "User",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                schema: "library",
                table: "Book",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Genre",
                schema: "library",
                table: "Book",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                schema: "library",
                table: "Book",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoomBook_BookCount_NonNegative",
                schema: "library",
                table: "RoomBook",
                sql: "\"BookCount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoomBook_BorrowedCount_LE_BookCount",
                schema: "library",
                table: "RoomBook",
                sql: "\"BorrowedCount\" <= \"BookCount\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoomBook_BorrowedCount_NonNegative",
                schema: "library",
                table: "RoomBook",
                sql: "\"BorrowedCount\" >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoomBook_RoomBook_RoomBookId",
                schema: "library",
                table: "UserRoomBook",
                column: "RoomBookId",
                principalSchema: "library",
                principalTable: "RoomBook",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoomBook_RoomBook_RoomBookId",
                schema: "library",
                table: "UserRoomBook");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RoomBook_BookCount_NonNegative",
                schema: "library",
                table: "RoomBook");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RoomBook_BorrowedCount_LE_BookCount",
                schema: "library",
                table: "RoomBook");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RoomBook_BorrowedCount_NonNegative",
                schema: "library",
                table: "RoomBook");

            migrationBuilder.AlterColumn<decimal>(
                name: "Penalty",
                schema: "library",
                table: "UserRoomBook",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.Sql("""
                ALTER TABLE library."User"
                ALTER COLUMN "Role" TYPE integer
                USING CASE "Role"
                    WHEN 'Reader' THEN 0
                    WHEN 'Librarian' THEN 1
                    WHEN 'Admin' THEN 2
                    ELSE 0
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                schema: "library",
                table: "User",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                schema: "library",
                table: "Book",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Genre",
                schema: "library",
                table: "Book",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                schema: "library",
                table: "Book",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoomBook_RoomBook_RoomBookId",
                schema: "library",
                table: "UserRoomBook",
                column: "RoomBookId",
                principalSchema: "library",
                principalTable: "RoomBook",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
