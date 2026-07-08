using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodDonation.API.Data;
using BloodDonation.API.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace BloodDonation.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DonationFormController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DonationFormController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitForm([FromBody] DonationFormSubmissionDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim.Value);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { Message = "User not found." });

            // Calculate age from DOB
            int age = 0;
            if (DateTime.TryParse(user.DateOfBirth, out var dob))
            {
                age = DateTime.UtcNow.Year - dob.Year;
                if (dob.Date > DateTime.UtcNow.AddYears(-age)) age--;
            }

            // Perform automatic eligibility rules validation
            bool isEligible = true;
            string reason = "";

            if (age < 18 || age > 65)
            {
                isEligible = false;
                reason += $"Age must be between 18 and 65 (Current: {age}). ";
            }
            if (dto.Weight < 50)
            {
                isEligible = false;
                reason += $"Weight must be at least 50 kg (Current: {dto.Weight} kg). ";
            }
            if (!dto.DontKnowVitals)
            {
                if (dto.Hemoglobin < 12.5 || dto.Hemoglobin > 18.0)
                {
                    isEligible = false;
                    reason += $"Hemoglobin level {dto.Hemoglobin} g/dL is outside the safe range (12.5 - 18.0). ";
                }
                if (dto.Hematocrit < 35 || dto.Hematocrit > 54)
                {
                    isEligible = false;
                    reason += $"Hematocrit level {dto.Hematocrit}% is outside the safe range (35% - 54%). ";
                }
            }

            // Check yes/no questions
            // Standard questionnaire questions:
            // Q1: Have you had a tattoo or piercing in the last 6 months? (Disqualifies temporarily)
            // Q2: Are you currently taking antibiotics or other medications? (Disqualifies temporarily)
            // Q3: Have you had surgery in the last 6 months? (Disqualifies temporarily)
            // Q4: Do you have a history of chronic infectious disease (HIV, Hepatitis)? (Disqualifies permanently)
            // Q5: For females: Are you currently pregnant or breastfeeding? (Disqualifies temporarily)
            
            if (dto.TattooOrPiercing)
            {
                isEligible = false;
                reason += "Had tattoo/piercing in the past 6 months. ";
            }
            if (dto.AntibioticsOrMedications)
            {
                isEligible = false;
                reason += "Currently taking medications/antibiotics. ";
            }
            if (dto.RecentSurgery)
            {
                isEligible = false;
                reason += "Had surgery in the past 6 months. ";
            }
            if (dto.ChronicDisease)
            {
                isEligible = false;
                reason += "History of chronic/infectious disease. ";
            }
            if (user.Gender.Equals("Female", StringComparison.OrdinalIgnoreCase) && dto.PregnantOrBreastfeeding)
            {
                isEligible = false;
                reason += "Currently pregnant or breastfeeding. ";
            }

            // Update user status
            string outcome = isEligible ? (dto.DontKnowVitals ? "PendingReview" : "Eligible") : "Ineligible";
            if (!isEligible)
            {
                if (dto.ChronicDisease)
                {
                    user.EligibilityStatus = "PermanentlyNotEligible";
                    user.EligibilityExpiryDate = null;
                }
                else
                {
                    user.EligibilityStatus = "TemporarilyNotEligible";
                    user.EligibilityExpiryDate = DateTime.UtcNow.AddMonths(6); // 6 months suspension
                }
            }
            else
            {
                // Set to Eligible or PendingReview
                user.EligibilityStatus = dto.DontKnowVitals ? "PendingReview" : "Eligible";
                user.EligibilityExpiryDate = null;
            }

            var questionsDict = new
            {
                dto.TattooOrPiercing,
                dto.AntibioticsOrMedications,
                dto.RecentSurgery,
                dto.ChronicDisease,
                dto.PregnantOrBreastfeeding
            };

            var form = new DonationForm
            {
                UserId = userId,
                SubmissionDate = DateTime.UtcNow,
                Age = age,
                Weight = dto.Weight,
                BloodGroup = dto.BloodGroup,
                RhFactor = dto.RhFactor,
                Hemoglobin = dto.DontKnowVitals ? 0 : dto.Hemoglobin,
                Hematocrit = dto.DontKnowVitals ? 0 : dto.Hematocrit,
                Address = dto.Address,
                MedicalConditions = dto.MedicalConditions,
                Medications = dto.Medications,
                RecentIllness = dto.RecentIllness,
                SurgeryHistory = dto.SurgeryHistory,
                PregnancyStatus = user.Gender.Equals("Female", StringComparison.OrdinalIgnoreCase) 
                    ? (dto.PregnantOrBreastfeeding ? "Pregnant/Breastfeeding" : "None") 
                    : "N/A",
                EligibilityQuestionsJson = JsonSerializer.Serialize(questionsDict),
                EligibilityResult = outcome,
                AdminNotes = !isEligible ? $"System flag: {reason}" : (dto.DontKnowVitals ? "Needs vitals check at center. Hemoglobin/Hematocrit not provided." : "Automatically approved by system checks.")
            };

            _context.DonationForms.Add(form);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Create notification for user
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = !isEligible 
                    ? $"Your donation screening did not pass. Status: {user.EligibilityStatus}. Reason: {reason}"
                    : (dto.DontKnowVitals 
                        ? "Form submitted! Your status is Pending Review. Please book an appointment to check your vitals at the center."
                        : "Your donation eligibility screening passed! You are now eligible to book appointments."),
                Type = !isEligible ? "Warning" : (dto.DontKnowVitals ? "Info" : "Success")
            });

            await _context.SaveChangesAsync();

            return Ok(new { EligibilityResult = outcome, Reason = reason, Status = user.EligibilityStatus });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim.Value);

            var history = await _context.DonationForms
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.SubmissionDate)
                .ToListAsync();

            return Ok(history);
        }

        // Admin Endpoints
        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllForms()
        {
            var forms = await _context.DonationForms
                .Include(f => f.User)
                .OrderByDescending(f => f.SubmissionDate)
                .ToListAsync();

            return Ok(forms.Select(f => new
            {
                f.Id,
                f.UserId,
                DonorName = f.User?.FullName,
                DonorNationalId = f.User?.NationalId,
                f.SubmissionDate,
                f.Age,
                f.Weight,
                f.BloodGroup,
                f.RhFactor,
                f.Hemoglobin,
                f.Hematocrit,
                f.Address,
                f.MedicalConditions,
                f.Medications,
                f.RecentIllness,
                f.SurgeryHistory,
                f.PregnancyStatus,
                f.EligibilityQuestionsJson,
                f.EligibilityResult,
                f.AdminNotes
            }));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/review")]
        public async Task<IActionResult> ReviewForm(int id, [FromBody] ReviewFormDto dto)
        {
            var form = await _context.DonationForms.Include(f => f.User).FirstOrDefaultAsync(f => f.Id == id);
            if (form == null) return NotFound(new { Message = "Form not found." });

            form.EligibilityResult = dto.EligibilityResult; // "Eligible" or "Ineligible"
            form.AdminNotes = dto.AdminNotes;

            if (form.User != null)
            {
                form.User.EligibilityStatus = dto.UserStatus; // "Eligible", "TemporarilyNotEligible", "PermanentlyNotEligible"
                if (dto.UserStatus == "TemporarilyNotEligible")
                {
                    form.User.EligibilityExpiryDate = DateTime.UtcNow.AddMonths(dto.SuspensionMonths > 0 ? dto.SuspensionMonths : 6);
                }
                else
                {
                    form.User.EligibilityExpiryDate = null;
                }
                _context.Users.Update(form.User);

                // Add notification
                _context.Notifications.Add(new Notification
                {
                    UserId = form.User.Id,
                    Message = $"Your donation eligibility form has been reviewed. Your status is now: {dto.UserStatus}. Notes: {dto.AdminNotes}",
                    Type = dto.EligibilityResult == "Eligible" ? "Success" : "Warning"
                });
            }

            _context.DonationForms.Update(form);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Form reviewed successfully." });
        }
    }

    public class DonationFormSubmissionDto
    {
        public double Weight { get; set; }
        public string BloodGroup { get; set; } = string.Empty;
        public string RhFactor { get; set; } = string.Empty;
        public double Hemoglobin { get; set; }
        public double Hematocrit { get; set; }
        public bool DontKnowVitals { get; set; }
        public string Address { get; set; } = string.Empty;
        public string MedicalConditions { get; set; } = string.Empty;
        public string Medications { get; set; } = string.Empty;
        public string RecentIllness { get; set; } = string.Empty;
        public string SurgeryHistory { get; set; } = string.Empty;
        
        // Questionnaire bools
        public bool TattooOrPiercing { get; set; }
        public bool AntibioticsOrMedications { get; set; }
        public bool RecentSurgery { get; set; }
        public bool ChronicDisease { get; set; }
        public bool PregnantOrBreastfeeding { get; set; }
    }

    public class ReviewFormDto
    {
        public string EligibilityResult { get; set; } = string.Empty; // "Eligible", "Ineligible"
        public string UserStatus { get; set; } = string.Empty; // "Eligible", "TemporarilyNotEligible", "PermanentlyNotEligible"
        public int SuspensionMonths { get; set; }
        public string AdminNotes { get; set; } = string.Empty;
    }
}
