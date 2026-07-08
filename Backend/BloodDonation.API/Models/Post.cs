using System;

namespace BloodDonation.API.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string Type { get; set; } = "Event"; // "Emergency" or "Event"
        
        // General fields
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = "Active"; // "Active", "Completed", "Archived"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Emergency Post Specifics
        public string BloodType { get; set; } = string.Empty; // e.g. "A+", "O-", "Any"
        public string UrgencyLevel { get; set; } = string.Empty; // "Low", "Medium", "High", "Emergency"
        public string ContactInfo { get; set; } = string.Empty;
        public int DonorsNeeded { get; set; }
        public DateTime? ExpiryTime { get; set; }

        // Event Post Specifics
        public DateTime? EventDate { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public int MaxCapacity { get; set; }
        public int AvailableSlots { get; set; }
    }
}
