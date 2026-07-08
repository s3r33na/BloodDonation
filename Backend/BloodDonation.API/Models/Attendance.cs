using System;

namespace BloodDonation.API.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        
        public int AppointmentId { get; set; }
        public Appointment? Appointment { get; set; }
        
        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
        public string VerifiedByAdminId { get; set; } = string.Empty;
        
        // Status: Waiting, Donating, Completed, Rejected
        public string Status { get; set; } = "Waiting"; 
        
        public string Notes { get; set; } = string.Empty;
    }
}
