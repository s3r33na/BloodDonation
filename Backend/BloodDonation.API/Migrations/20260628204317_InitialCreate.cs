using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BloodDonation.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BloodType = table.Column<string>(type: "TEXT", nullable: false),
                    UrgencyLevel = table.Column<string>(type: "TEXT", nullable: false),
                    ContactInfo = table.Column<string>(type: "TEXT", nullable: false),
                    DonorsNeeded = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiryTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EventDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MaxCapacity = table.Column<int>(type: "INTEGER", nullable: false),
                    AvailableSlots = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    NationalId = table.Column<string>(type: "TEXT", nullable: false),
                    MobileNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    DateOfBirth = table.Column<string>(type: "TEXT", nullable: false),
                    Gender = table.Column<string>(type: "TEXT", nullable: false),
                    Nationality = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    EligibilityStatus = table.Column<string>(type: "TEXT", nullable: false),
                    EligibilityExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    PostId = table.Column<int>(type: "INTEGER", nullable: false),
                    AppointmentDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    QrCodeToken = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Appointments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DonationForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    BloodGroup = table.Column<string>(type: "TEXT", nullable: false),
                    RhFactor = table.Column<string>(type: "TEXT", nullable: false),
                    Hemoglobin = table.Column<double>(type: "REAL", nullable: false),
                    Hematocrit = table.Column<double>(type: "REAL", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    MedicalConditions = table.Column<string>(type: "TEXT", nullable: false),
                    Medications = table.Column<string>(type: "TEXT", nullable: false),
                    RecentIllness = table.Column<string>(type: "TEXT", nullable: false),
                    SurgeryHistory = table.Column<string>(type: "TEXT", nullable: false),
                    PregnancyStatus = table.Column<string>(type: "TEXT", nullable: false),
                    EligibilityQuestionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    AdminNotes = table.Column<string>(type: "TEXT", nullable: false),
                    EligibilityResult = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonationForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonationForms_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppointmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VerifiedByAdminId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendances_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Posts",
                columns: new[] { "Id", "AvailableSlots", "BloodType", "ContactInfo", "CreatedAt", "Description", "DonorsNeeded", "EventDate", "ExpiryTime", "Location", "MaxCapacity", "Status", "Title", "Type", "UrgencyLevel" },
                values: new object[,]
                {
                    { 1, 48, "", "", new DateTime(2026, 6, 28, 20, 43, 16, 420, DateTimeKind.Utc).AddTicks(8311), "Help us restore critical blood reserves. Join the national drive at Al-Hussein Park.", 0, new DateTime(2026, 7, 15, 9, 0, 0, 0, DateTimeKind.Utc), null, "Al-Hussein Park, Amman", 50, "Active", "Amman Central Blood Drive", "Event", "" },
                    { 2, 30, "", "", new DateTime(2026, 6, 28, 20, 43, 16, 420, DateTimeKind.Utc).AddTicks(8686), "Your blood donation can save three lives. Come and contribute.", 0, new DateTime(2026, 7, 20, 10, 0, 0, 0, DateTimeKind.Utc), null, "King Abdullah II Gardens, Irbid", 30, "Active", "Irbid Community Donation Event", "Event", "" },
                    { 3, 0, "O-", "0790000001", new DateTime(2026, 6, 28, 20, 43, 16, 421, DateTimeKind.Utc).AddTicks(1355), "A patient is undergoing open-heart bypass surgery and requires immediate O- negative blood donations.", 3, null, new DateTime(2026, 7, 10, 23, 59, 59, 0, DateTimeKind.Utc), "Specialty Hospital, Amman", 0, "Active", "Urgent O- Required at Specialty Hospital", "Emergency", "Emergency" },
                    { 4, 0, "A+", "0790000002", new DateTime(2026, 6, 28, 20, 43, 16, 421, DateTimeKind.Utc).AddTicks(1380), "Platelet donation needed for a leukemia patient undergoing chemotherapy at KHCC.", 5, null, new DateTime(2026, 7, 12, 12, 0, 0, 0, DateTimeKind.Utc), "King Hussein Cancer Center, Amman", 0, "Active", "A+ Donors Needed for Leukemia Patient", "Emergency", "High" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "DateOfBirth", "EligibilityExpiryDate", "EligibilityStatus", "Email", "FullName", "Gender", "MobileNumber", "NationalId", "Nationality", "PasswordHash", "Role" },
                values: new object[] { 1, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "1990-01-01", null, "Eligible", "admin@blood.jo", "National Blood Bank Admin", "Male", "0791234567", "9901020304", "Jordanian", "342IHFCmkWx+4Auu8lDJhQoxcv3QA/pfVNsGeKqEGFo=", "Admin" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PostId",
                table: "Appointments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserId",
                table: "Appointments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_AppointmentId",
                table: "Attendances",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationForms_UserId",
                table: "DonationForms",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "DonationForms");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
