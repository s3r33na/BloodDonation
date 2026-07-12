using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodDonation.API.Data;
using BloodDonation.API.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace BloodDonation.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AppointmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("book")]
        public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim.Value);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { Message = "User not found." });

            // Constraint: 1 & 3: Ineligible users blocked from booking
            if (user.EligibilityStatus == "PermanentlyNotEligible" || user.EligibilityStatus == "TemporarilyNotEligible")
            {
                if (user.EligibilityExpiryDate.HasValue && user.EligibilityExpiryDate.Value > DateTime.UtcNow)
                {
                    return BadRequest(new { Message = $"Booking restricted. Your eligibility suspension is active until {user.EligibilityExpiryDate.Value.ToLocalTime():yyyy-MM-dd} due to previous screening checks." });
                }
                else if (user.EligibilityStatus == "PermanentlyNotEligible")
                {
                    return BadRequest(new { Message = "Booking restricted. You are permanently deferred from donating blood under regulatory rules." });
                }
            }

            // Check if user already has an active booking for this event
            var existingAppt = await _context.Appointments
                .FirstOrDefaultAsync(a => a.UserId == userId && a.PostId == dto.PostId && (a.Status == "Booked" || a.Status == "CheckedIn"));
            if (existingAppt != null)
            {
                return BadRequest(new { Message = "You already have an active appointment booked for this event." });
            }

            var post = await _context.Posts.FindAsync(dto.PostId);
            if (post == null || post.Type != "Event")
            {
                return BadRequest(new { Message = "Invalid event." });
            }

            if (post.Status != "Active")
            {
                return BadRequest(new { Message = "This event is no longer active." });
            }

            // Validate that the chosen booking date/time is within the event's start and end times
            if (post.StartDateTime.HasValue && dto.AppointmentDateTime < post.StartDateTime.Value)
            {
                return BadRequest(new { Message = $"Appointment time cannot be before the event starts ({post.StartDateTime.Value.ToLocalTime():yyyy-MM-dd HH:mm})." });
            }
            if (post.EndDateTime.HasValue && dto.AppointmentDateTime > post.EndDateTime.Value)
            {
                return BadRequest(new { Message = $"Appointment time cannot be after the event ends ({post.EndDateTime.Value.ToLocalTime():yyyy-MM-dd HH:mm})." });
            }

            // Validate Screening questionnaire answers
            if (dto.Screening == null)
            {
                return BadRequest(new { Message = "Screening questionnaire answers are required to book an appointment." });
            }

            // 1. Consent Validation
            if (!dto.Screening.ConsentBloodDraw || !dto.Screening.ConsentDeclaration)
            {
                return BadRequest(new { Message = "Consent to blood draw and truthfulness declaration are mandatory to complete booking." });
            }

            // 2. Eligibility evaluation
            bool isEligible = true;
            List<string> reasons = new();

            if (!dto.Screening.FeelGoodHealth)
            {
                isEligible = false;
                reasons.Add("Not feeling in good health currently.");
            }
            if (dto.Screening.BeenRejected)
            {
                isEligible = false;
                reasons.Add("Has history of being rejected as a donor.");
            }
            if (dto.Screening.HadJaundiceOrHepatitis)
            {
                isEligible = false;
                reasons.Add("Has history of jaundice or hepatitis (Permanent deferral).");
            }
            if (dto.Screening.HadMalaria)
            {
                isEligible = false;
                reasons.Add("Has history of malaria.");
            }
            if (dto.Screening.HadBrucellosisOrTyphoid)
            {
                isEligible = false;
                reasons.Add("Has history of brucellosis or typhoid fever.");
            }
            if (dto.Screening.SideEffectsPrevious)
            {
                isEligible = false;
                reasons.Add("Experienced side effects after previous donation.");
            }
            if (dto.Screening.TraveledLast6Months)
            {
                isEligible = false;
                reasons.Add("Traveled outside Jordan in the last six months.");
            }
            if (dto.Screening.ChronicDiseases != null && dto.Screening.ChronicDiseases.Count > 0)
            {
                isEligible = false;
                reasons.Add($"Suffer from chronic disease: {string.Join(", ", dto.Screening.ChronicDiseases)}.");
            }
            if (dto.Screening.TakingMedicationsOrInjections)
            {
                isEligible = false;
                reasons.Add("Currently taking medications or medications via injection.");
            }
            if (dto.Screening.FaintingSpellsSeizures)
            {
                isEligible = false;
                reasons.Add("Suffers from fainting spells or seizures.");
            }
            if (dto.Screening.SevereAllergies)
            {
                isEligible = false;
                reasons.Add("Suffers from severe allergies.");
            }
            if (dto.Screening.ReceivedVaccine14Days)
            {
                isEligible = false;
                reasons.Add("Received a vaccine in the past 14 days.");
            }
            if (dto.Screening.TattooOrCupping12Months)
            {
                isEligible = false;
                reasons.Add("Had a tattoo or cupping in the past 12 months.");
            }
            if (dto.Screening.SurgeryOrTransfusion12Months)
            {
                isEligible = false;
                reasons.Add("Had surgery or received a blood transfusion in the past 12 months.");
            }
            if (dto.Screening.DentistLastWeek)
            {
                isEligible = false;
                reasons.Add("Visited a dentist in the past week.");
            }
            if (user.Gender.Equals("Female", StringComparison.OrdinalIgnoreCase) && 
                dto.Screening.FemalePregnancyStatus != null && 
                dto.Screening.FemalePregnancyStatus.Count > 0)
            {
                isEligible = false;
                reasons.Add($"Female specific deferral conditions met: {string.Join(", ", dto.Screening.FemalePregnancyStatus)}.");
            }
            if (!dto.Screening.RecommendDonatedBlood)
            {
                isEligible = false;
                reasons.Add("Did not recommend giving the blood unit to patients.");
            }
            if (!dto.Screening.ReceivedEducationalInfo)
            {
                isEligible = false;
                reasons.Add("Did not receive educational info regarding blood donation.");
            }

            // Update user status
            if (!isEligible)
            {
                bool isPermanent = dto.Screening.HadJaundiceOrHepatitis || 
                                   dto.Screening.HadMalaria || 
                                   (dto.Screening.ChronicDiseases != null && 
                                    (dto.Screening.ChronicDiseases.Contains("Heart") || 
                                     dto.Screening.ChronicDiseases.Contains("Kidneys") || 
                                     dto.Screening.ChronicDiseases.Contains("Lungs")));

                user.EligibilityStatus = isPermanent ? "PermanentlyNotEligible" : "TemporarilyNotEligible";
                user.EligibilityExpiryDate = isPermanent ? null : DateTime.UtcNow.AddMonths(3); // 3 months suspension
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return BadRequest(new { Message = $"Booking denied. Medical screening ineligibility: {string.Join(" ", reasons)}" });
            }
            else
            {
                user.EligibilityStatus = "Eligible";
                user.EligibilityExpiryDate = null;
                _context.Users.Update(user);
            }

            // Generate a unique 6-digit QR Token
            string qrToken;
            do
            {
                qrToken = Random.Shared.Next(100000, 999999).ToString();
            } while (await _context.Appointments.AnyAsync(a => a.QrCodeToken == qrToken));

            var appointment = new Appointment
            {
                UserId = userId,
                PostId = post.Id,
                AppointmentDateTime = dto.AppointmentDateTime,
                Status = "Booked",
                QrCodeToken = qrToken,
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Calculate age
            int age = 0;
            if (DateTime.TryParse(user.DateOfBirth, out var dob))
            {
                age = DateTime.UtcNow.Year - dob.Year;
                if (dob.Date > DateTime.UtcNow.AddYears(-age)) age--;
            }

            // Split user.BloodType into BloodGroup and RhFactor
            string bloodGroup = "Unknown";
            string rhFactor = "";
            if (!string.IsNullOrEmpty(user.BloodType))
            {
                if (user.BloodType.EndsWith("+") || user.BloodType.EndsWith("-"))
                {
                    bloodGroup = user.BloodType.Substring(0, user.BloodType.Length - 1);
                    rhFactor = user.BloodType.Substring(user.BloodType.Length - 1);
                }
                else
                {
                    bloodGroup = user.BloodType;
                }
            }

            // Save DonationForm (Screening record) linked to this Appointment
            var form = new DonationForm
            {
                UserId = userId,
                AppointmentId = appointment.Id,
                SubmissionDate = DateTime.UtcNow,
                Age = age,
                Weight = 70.0, // Default baseline weight, vitals to be adjusted by staff
                BloodGroup = bloodGroup,
                RhFactor = rhFactor,
                Hemoglobin = 0,
                Hematocrit = 0,
                Address = "الأردن",
                MedicalConditions = dto.Screening.ChronicDiseases != null && dto.Screening.ChronicDiseases.Count > 0 ? string.Join(", ", dto.Screening.ChronicDiseases) : "None",
                Medications = dto.Screening.TakingMedicationsOrInjections ? "Yes" : "None",
                RecentIllness = dto.Screening.FeelGoodHealth ? "None" : "Not feeling well",
                SurgeryHistory = dto.Screening.SurgeryOrTransfusion12Months ? "Surgery in past 12 months" : "None",
                PregnancyStatus = user.Gender.Equals("Female", StringComparison.OrdinalIgnoreCase) && dto.Screening.FemalePregnancyStatus != null && dto.Screening.FemalePregnancyStatus.Count > 0 ? string.Join(", ", dto.Screening.FemalePregnancyStatus) : "None",
                EligibilityQuestionsJson = JsonSerializer.Serialize(dto.Screening),
                EligibilityResult = "Eligible",
                AdminNotes = "Approved via booking questionnaire. Vitals check required at center."
            };

            _context.DonationForms.Add(form);

            // Create notification for user
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = $"Appointment booked successfully for {post.Title} on {appointment.AppointmentDateTime:yyyy-MM-dd HH:mm}. QR Code: {qrToken}.",
                Type = "Success"
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Appointment booked successfully.",
                AppointmentId = appointment.Id,
                QrCodeToken = qrToken,
                AppointmentDateTime = appointment.AppointmentDateTime
            });
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMyAppointments()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim.Value);

            var appointments = await _context.Appointments
                .Include(a => a.Post)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();

            return Ok(appointments.Select(a => new
            {
                a.Id,
                a.PostId,
                EventName = a.Post?.Title,
                EventLocation = a.Post?.Location,
                a.AppointmentDateTime,
                a.Status,
                a.QrCodeToken,
                a.CreatedAt
            }));
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);
            bool isAdmin = roleClaim?.Value == "Admin";

            var appointment = await _context.Appointments.Include(a => a.Post).FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null) return NotFound(new { Message = "Appointment not found." });

            // Validate ownership
            if (!isAdmin && appointment.UserId != userId)
            {
                return Forbid();
            }

            if (appointment.Status == "Canceled")
            {
                return BadRequest(new { Message = "Appointment is already canceled." });
            }

            if (appointment.Status == "Completed" || appointment.Status == "CheckedIn")
            {
                return BadRequest(new { Message = "Cannot cancel an appointment that has been checked in or completed." });
            }

            // Restore slot
            if (appointment.Post != null && appointment.Post.Type != "Event")
            {
                appointment.Post.AvailableSlots++;
                _context.Posts.Update(appointment.Post);
            }

            appointment.Status = "Canceled";
            _context.Appointments.Update(appointment);

            // Notify user
            _context.Notifications.Add(new Notification
            {
                UserId = appointment.UserId,
                Message = $"Your appointment for '{appointment.Post?.Title}' has been canceled.",
                Type = "Info"
            });

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Appointment canceled successfully." });
        }

        // Admin Endpoints
        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllAppointments([FromQuery] string? search, [FromQuery] string? status)
        {
            var query = _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Post)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(a => a.Status == status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => a.User!.FullName.Contains(search) || 
                                         a.User.NationalId.Contains(search) || 
                                         a.Post!.Title.Contains(search));
            }

            var appts = await query.OrderByDescending(a => a.AppointmentDateTime).ToListAsync();

            return Ok(appts.Select(a => new
            {
                a.Id,
                a.UserId,
                DonorName = a.User?.FullName,
                DonorNationalId = a.User?.NationalId,
                DonorPhone = a.User?.MobileNumber,
                DonorBloodType = _context.DonationForms.Where(df => df.UserId == a.UserId).OrderByDescending(df => df.SubmissionDate).Select(df => df.BloodGroup + df.RhFactor).FirstOrDefault() ?? "Unknown",
                a.PostId,
                EventName = a.Post?.Title,
                a.AppointmentDateTime,
                a.Status,
                a.QrCodeToken,
                a.CheckedInAt
            }));
        }

        // QR Check-In verification endpoint
        [Authorize(Roles = "Admin")]
        [HttpPost("check-in")]
        public async Task<IActionResult> CheckIn([FromBody] CheckInDto dto)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            string adminId = adminIdClaim?.Value ?? "Admin";

            var appointment = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Post)
                .FirstOrDefaultAsync(a => a.QrCodeToken == dto.QrCodeToken);

            if (appointment == null)
            {
                return NotFound(new { Message = "Invalid QR Code. Appointment not found." });
            }

            if (appointment.Status == "Canceled")
            {
                return BadRequest(new { Message = "Check-in failed. Appointment is canceled." });
            }

            if (appointment.Status == "CheckedIn" || appointment.Status == "Completed")
            {
                return BadRequest(new { Message = "Duplicate scan. Donor is already checked in or donation is completed." });
            }

            // Check if there is already an attendance entry
            var existingAttendance = await _context.Attendances.AnyAsync(at => at.AppointmentId == appointment.Id);
            if (existingAttendance)
            {
                return BadRequest(new { Message = "Duplicate scan detected. Attendance list already has this appointment." });
            }

            // Success validation!
            appointment.Status = "CheckedIn";
            appointment.CheckedInAt = DateTime.UtcNow;

            var attendance = new Attendance
            {
                AppointmentId = appointment.Id,
                CheckInTime = DateTime.UtcNow,
                VerifiedByAdminId = adminId,
                Status = "Waiting", // Donor is waiting for medical team
                Notes = "Checked in via QR Code."
            };

            _context.Appointments.Update(appointment);
            _context.Attendances.Add(attendance);
            
            // Add user notification
            _context.Notifications.Add(new Notification
            {
                UserId = appointment.UserId,
                Message = $"You have checked in at '{appointment.Post?.Title}'. Please proceed to the waiting area.",
                Type = "Success"
            });

            await _context.SaveChangesAsync();

            // Fetch donor's blood group
            var bloodGroup = await _context.DonationForms
                .Where(df => df.UserId == appointment.UserId)
                .OrderByDescending(df => df.SubmissionDate)
                .Select(df => df.BloodGroup + df.RhFactor)
                .FirstOrDefaultAsync() ?? "Not set";

            return Ok(new
            {
                Message = "Check-in successful!",
                DonorName = appointment.User?.FullName,
                NationalId = appointment.User?.NationalId,
                MobileNumber = appointment.User?.MobileNumber,
                BloodGroup = bloodGroup,
                EventName = appointment.Post?.Title,
                CheckInTime = attendance.CheckInTime,
                AppointmentId = appointment.Id
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/attendance-status")]
        public async Task<IActionResult> UpdateAttendanceStatus(int id, [FromBody] UpdateAttendanceDto dto)
        {
            var appointment = await _context.Appointments.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null) return NotFound(new { Message = "Appointment not found." });

            var attendance = await _context.Attendances.FirstOrDefaultAsync(at => at.AppointmentId == id);
            if (attendance == null) return NotFound(new { Message = "Attendance record not found." });

            attendance.Status = dto.Status; // "Waiting", "Donating", "Completed", "Rejected"
            attendance.Notes = dto.Notes;

            if (dto.Status == "Completed")
            {
                appointment.Status = "Completed";
                
                // Suspend donor from registering new appointments for 3 months
                if (appointment.User != null)
                {
                    appointment.User.EligibilityStatus = "TemporarilyNotEligible";
                    appointment.User.EligibilityExpiryDate = DateTime.UtcNow.AddMonths(3);
                    _context.Users.Update(appointment.User);
                }

                _context.Notifications.Add(new Notification
                {
                    UserId = appointment.UserId,
                    Message = "Thank you! Your donation was marked as Completed. You will be eligible to donate again in 3 months.",
                    Type = "Success"
                });
            }
            else if (dto.Status == "Rejected")
            {
                // Donor was rejected by medical staff at the event
                appointment.Status = "Completed"; // Appointment processed but rejected
                if (appointment.User != null)
                {
                    appointment.User.EligibilityStatus = "TemporarilyNotEligible";
                    appointment.User.EligibilityExpiryDate = DateTime.UtcNow.AddMonths(1); // Suspend for 1 month
                    _context.Users.Update(appointment.User);
                }

                _context.Notifications.Add(new Notification
                {
                    UserId = appointment.UserId,
                    Message = $"Your donation process was rejected at the venue. Reason: {dto.Notes}.",
                    Type = "Warning"
                });
            }

            _context.Appointments.Update(appointment);
            _context.Attendances.Update(attendance);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Attendance status updated successfully." });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/resend")]
        public async Task<IActionResult> ResendDetails(int id)
        {
            var appointment = await _context.Appointments.Include(a => a.User).Include(a => a.Post).FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null) return NotFound(new { Message = "Appointment not found." });

            // Simulate email / sms sending by creating notifications
            _context.Notifications.Add(new Notification
            {
                UserId = appointment.UserId,
                Message = $"[RESENT] Booking confirmation for '{appointment.Post?.Title}' on {appointment.AppointmentDateTime:yyyy-MM-dd HH:mm}. QR Code: {appointment.QrCodeToken}",
                Type = "Info"
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Appointment details resent successfully." });
        }
    }

    public class ScreeningDto
    {
        public bool FeelGoodHealth { get; set; }
        public bool BeenRejected { get; set; }
        public bool HadJaundiceOrHepatitis { get; set; }
        public bool HadMalaria { get; set; }
        public bool HadBrucellosisOrTyphoid { get; set; }
        public bool SideEffectsPrevious { get; set; }
        public bool TraveledLast6Months { get; set; }
        public List<string> ChronicDiseases { get; set; } = new();
        public bool TakingMedicationsOrInjections { get; set; }
        public bool FaintingSpellsSeizures { get; set; }
        public bool SevereAllergies { get; set; }
        public bool ReceivedVaccine14Days { get; set; }
        public bool TattooOrCupping12Months { get; set; }
        public bool SurgeryOrTransfusion12Months { get; set; }
        public bool DentistLastWeek { get; set; }
        public List<string> FemalePregnancyStatus { get; set; } = new();
        public bool ConsentBloodDraw { get; set; }
        public bool ConsentDeclaration { get; set; }
        public bool RecommendDonatedBlood { get; set; }
        public bool ReceivedEducationalInfo { get; set; }
    }

    public class BookAppointmentDto
    {
        public int PostId { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public ScreeningDto Screening { get; set; } = null!;
    }

    public class CheckInDto
    {
        public string QrCodeToken { get; set; } = string.Empty;
    }

    public class UpdateAttendanceDto
    {
        public string Status { get; set; } = string.Empty; // "Waiting", "Donating", "Completed", "Rejected"
        public string Notes { get; set; } = string.Empty;
    }
}
