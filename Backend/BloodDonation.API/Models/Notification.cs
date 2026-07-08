using System;

namespace BloodDonation.API.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        public int? UserId { get; set; } // Null if it is a general system notification for Admins
        public User? User { get; set; }
        
        public string Message { get; set; } = string.Empty;
        
        // Type: Info, Success, Warning, Alert
        public string Type { get; set; } = "Info"; 
        
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
