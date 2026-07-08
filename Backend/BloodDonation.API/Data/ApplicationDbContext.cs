using Microsoft.EntityFrameworkCore;
using BloodDonation.API.Models;
using System;
using System.Security.Cryptography;
using System.Text;

namespace BloodDonation.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<DonationForm> DonationForms => Set<DonationForm>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<DonationForm>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Post)
                .WithMany()
                .HasForeignKey(a => a.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Attendance>()
                .HasOne(at => at.Appointment)
                .WithMany()
                .HasForeignKey(at => at.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Hash a default password for the admin user
            string adminPasswordHash = HashPassword("AdminPassword123");

            // Seed Admin User
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FullName = "National Blood Bank Admin",
                    NationalId = "9901020304",
                    MobileNumber = "0791234567",
                    Email = "admin@blood.jo",
                    PasswordHash = adminPasswordHash,
                    DateOfBirth = "1990-01-01",
                    Gender = "Male",
                    Nationality = "Jordanian",
                    Role = "Admin",
                    EligibilityStatus = "Eligible",
                    CreatedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Seed Posts (Events & Emergencies)
            modelBuilder.Entity<Post>().HasData(
                new Post
                {
                    Id = 1,
                    Type = "Event",
                    Title = "Amman Central Blood Drive",
                    Description = "Help us restore critical blood reserves. Join the national drive at Al-Hussein Park.",
                    Location = "Al-Hussein Park, Amman",
                    Status = "Active",
                    MaxCapacity = 50,
                    AvailableSlots = 48,
                    EventDate = new DateTime(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc),
                    StartDateTime = new DateTime(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc),
                    EndDateTime = new DateTime(2026, 7, 17, 17, 0, 0, DateTimeKind.Utc),
                    CreatedAt = DateTime.UtcNow
                },
                new Post
                {
                    Id = 2,
                    Type = "Event",
                    Title = "Irbid Community Donation Event",
                    Description = "Your blood donation can save three lives. Come and contribute.",
                    Location = "King Abdullah II Gardens, Irbid",
                    Status = "Active",
                    MaxCapacity = 30,
                    AvailableSlots = 30,
                    EventDate = new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc),
                    StartDateTime = new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc),
                    EndDateTime = new DateTime(2026, 7, 20, 18, 0, 0, DateTimeKind.Utc),
                    CreatedAt = DateTime.UtcNow
                },
                new Post
                {
                    Id = 3,
                    Type = "Emergency",
                    Title = "Urgent O- Required at Specialty Hospital",
                    Description = "A patient is undergoing open-heart bypass surgery and requires immediate O- negative blood donations.",
                    Location = "Specialty Hospital, Amman",
                    Status = "Active",
                    BloodType = "O-",
                    UrgencyLevel = "Emergency",
                    ContactInfo = "0790000001",
                    DonorsNeeded = 3,
                    ExpiryTime = new DateTime(2026, 7, 10, 23, 59, 59, DateTimeKind.Utc),
                    CreatedAt = DateTime.UtcNow
                },
                new Post
                {
                    Id = 4,
                    Type = "Emergency",
                    Title = "A+ Donors Needed for Leukemia Patient",
                    Description = "Platelet donation needed for a leukemia patient undergoing chemotherapy at KHCC.",
                    Location = "King Hussein Cancer Center, Amman",
                    Status = "Active",
                    BloodType = "A+",
                    UrgencyLevel = "High",
                    ContactInfo = "0790000002",
                    DonorsNeeded = 5,
                    ExpiryTime = new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc),
                    CreatedAt = DateTime.UtcNow
                }
            );
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
