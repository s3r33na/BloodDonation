using System;

namespace BloodDonation.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // "User" or "Admin"
        public string EligibilityStatus { get; set; } = "PendingReview"; // "Eligible", "TemporarilyNotEligible", "PermanentlyNotEligible", "PendingReview"
        public DateTime? EligibilityExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
