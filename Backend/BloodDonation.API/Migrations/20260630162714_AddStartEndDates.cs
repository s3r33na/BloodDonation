using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodDonation.API.Migrations
{
    /// <inheritdoc />
    public partial class AddStartEndDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDateTime",
                table: "Posts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDateTime",
                table: "Posts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "EndDateTime", "StartDateTime" },
                values: new object[] { new DateTime(2026, 6, 30, 16, 27, 13, 142, DateTimeKind.Utc).AddTicks(762), new DateTime(2026, 7, 17, 17, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 15, 9, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "EndDateTime", "StartDateTime" },
                values: new object[] { new DateTime(2026, 6, 30, 16, 27, 13, 142, DateTimeKind.Utc).AddTicks(1144), new DateTime(2026, 7, 20, 18, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 20, 10, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "EndDateTime", "StartDateTime" },
                values: new object[] { new DateTime(2026, 6, 30, 16, 27, 13, 142, DateTimeKind.Utc).AddTicks(3124), null, null });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "EndDateTime", "StartDateTime" },
                values: new object[] { new DateTime(2026, 6, 30, 16, 27, 13, 142, DateTimeKind.Utc).AddTicks(3130), null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDateTime",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "StartDateTime",
                table: "Posts");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 28, 20, 43, 16, 420, DateTimeKind.Utc).AddTicks(8311));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 28, 20, 43, 16, 420, DateTimeKind.Utc).AddTicks(8686));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 28, 20, 43, 16, 421, DateTimeKind.Utc).AddTicks(1355));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 28, 20, 43, 16, 421, DateTimeKind.Utc).AddTicks(1380));
        }
    }
}
