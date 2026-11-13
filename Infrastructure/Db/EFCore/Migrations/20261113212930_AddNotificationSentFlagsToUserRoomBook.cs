using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibApp.Infrastructure.Db.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSentFlagsToUserRoomBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OverdueNoticeSentAt",
                schema: "library",
                table: "UserRoomBook",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAt",
                schema: "library",
                table: "UserRoomBook",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OverdueNoticeSentAt",
                schema: "library",
                table: "UserRoomBook");

            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                schema: "library",
                table: "UserRoomBook");
        }
    }
}
