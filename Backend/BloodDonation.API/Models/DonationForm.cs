using System;

namespace BloodDonation.API.Models
{
    public class DonationForm
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        
        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;
        
        // Medical parameters
        public int Age { get; set; }
        public double Weight { get; set; }
        public string BloodGroup { get; set; } = string.Empty; // A, B, AB, O, etc.
        public string RhFactor { get; set; } = string.Empty; // +, -
        public double Hemoglobin { get; set; }
        public double Hematocrit { get; set; }
        public string Address { get; set; } = string.Empty;
        
        // Screening
        public string MedicalConditions { get; set; } = string.Empty;
        public string Medications { get; set; } = string.Empty;
        public string RecentIllness { get; set; } = string.Empty;
        public string SurgeryHistory { get; set; } = string.Empty;
        public string PregnancyStatus { get; set; } = string.Empty; // e.g., "N/A", "Pregnant", "Breastfeeding", "None"
        
        // JSON storage of yes/no eligibility answers
        public string EligibilityQuestionsJson { get; set; } = string.Empty;
        
        // Review outcomes
        public string AdminNotes { get; set; } = string.Empty;
        public string EligibilityResult { get; set; } = "PendingReview"; // "Eligible", "Ineligible"
    }
}
