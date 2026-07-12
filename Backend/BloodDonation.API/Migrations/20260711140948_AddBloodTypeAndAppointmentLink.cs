using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodDonation.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBloodTypeAndAppointmentLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BloodType",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AppointmentId",
                table: "DonationForms",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 11, 14, 9, 48, 148, DateTimeKind.Utc).AddTicks(914));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 11, 14, 9, 48, 148, DateTimeKind.Utc).AddTicks(1166));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 11, 14, 9, 48, 148, DateTimeKind.Utc).AddTicks(2335));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 11, 14, 9, 48, 148, DateTimeKind.Utc).AddTicks(2339));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "BloodType",
                value: "");

            migrationBuilder.CreateIndex(
                name: "IX_DonationForms_AppointmentId",
                table: "DonationForms",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_DonationForms_Appointments_AppointmentId",
                table: "DonationForms",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonationForms_Appointments_AppointmentId",
                table: "DonationForms");

            migrationBuilder.DropIndex(
                name: "IX_DonationForms_AppointmentId",
                table: "DonationForms");

            migrationBuilder.DropColumn(
                name: "BloodType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "DonationForms");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 30, 16, 27, 13, 142, DateTimeKind.Utc).AddTicks(762));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 30, 16, 27, 13, 142, DateTimeKind.Utc).AddTicks(1144));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 30, 16, 27, 13, 142, DateTimeKind.Utc).AddTicks(3124));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 30, 16, 27, 13, 142, DateTimeKind.Utc).AddTicks(3130));
        }
    }
}
