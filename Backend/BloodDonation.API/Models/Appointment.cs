using System;

namespace BloodDonation.API.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        
        public int PostId { get; set; }
        public Post? Post { get; set; }
        
        public DateTime AppointmentDateTime { get; set; }
        
        // Status: Booked, CheckedIn, Completed, Canceled, NoShow
        public string Status { get; set; } = "Booked"; 
        
        public string QrCodeToken { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CheckedInAt { get; set; }
    }
}
