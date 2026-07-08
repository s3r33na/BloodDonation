using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodDonation.API.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using BloodDonation.API.Models;
namespace BloodDonation.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStats()
        {
            var totalUsers = await _context.Users.CountAsync(u => u.Role == "User");
            var totalForms = await _context.DonationForms.CountAsync();
            var totalEvents = await _context.Posts.CountAsync(p => p.Type == "Event" && p.Status == "Active");
            var totalEmergencies = await _context.Posts.CountAsync(p => p.Type == "Emergency" && p.Status == "Active");
            
            var totalAppointments = await _context.Appointments.CountAsync();
            var bookedAppointments = await _context.Appointments.CountAsync(a => a.Status == "Booked");
            var checkedInAppointments = await _context.Appointments.CountAsync(a => a.Status == "CheckedIn");
            var completedAppointments = await _context.Appointments.CountAsync(a => a.Status == "Completed");
            var canceledAppointments = await _context.Appointments.CountAsync(a => a.Status == "Canceled");
            var noShowAppointments = await _context.Appointments.CountAsync(a => a.Status == "NoShow");

            var totalAttendees = await _context.Attendances.CountAsync(at => at.Status != "Rejected");
            var confirmedDonations = await _context.Attendances.CountAsync(at => at.Status == "Completed");

            return Ok(new
            {
                TotalRegisteredUsers = totalUsers,
                NewSubmissions = totalForms,
                UpcomingEvents = totalEvents,
                EmergencyRequests = totalEmergencies,
                TotalAppointments = totalAppointments,
                BookedAppointments = bookedAppointments,
                CheckedInAppointments = checkedInAppointments,
                CompletedAppointments = completedAppointments,
                CanceledAppointments = canceledAppointments,
                NoShowAppointments = noShowAppointments,
                EventAttendees = totalAttendees,
                ConfirmedDonations = confirmedDonations
            });
        }

        [HttpGet("charts")]
        public async Task<IActionResult> GetChartsData()
        {
            var now = DateTime.UtcNow;
            
            // 1. New users per time period (last 7 days)
            var lastWeek = now.AddDays(-7);
            var usersTimeline = await _context.Users
                .Where(u => u.Role == "User" && u.CreatedAt >= lastWeek)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), Count = g.Count() })
                .ToListAsync();

            // 2. Booked vs Attendee counts
            var appointmentsTimeline = await _context.Appointments
                .GroupBy(a => a.AppointmentDateTime.Date)
                .Select(g => new 
                { 
                    Date = g.Key.ToString("yyyy-MM-dd"), 
                    Booked = g.Count(),
                    CheckedIn = g.Count(a => a.Status == "CheckedIn" || a.Status == "Completed")
                })
                .Take(7)
                .ToListAsync();

            // 3. Emergency posts created in the last 30 days
            var emergencyCount = await _context.Posts.CountAsync(p => p.Type == "Emergency" && p.CreatedAt >= now.AddDays(-30));

            // 4. Blood type availability (registered donors' blood types)
            var bloodAvailability = await _context.DonationForms
                .GroupBy(df => df.BloodGroup + df.RhFactor)
                .Select(g => new { BloodType = g.Key, Count = g.Select(x => x.UserId).Distinct().Count() })
                .ToListAsync();

            // Fill empty blood types if needed
            var bloodTypes = new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
            var bloodAvailabilityList = bloodTypes.Select(type => new
            {
                BloodType = type,
                Count = bloodAvailability.FirstOrDefault(b => b.BloodType == type)?.Count ?? 0
            }).ToList();

            // 5. Blood type demand (Emergency request sum of DonorsNeeded)
            var bloodDemand = await _context.Posts
                .Where(p => p.Type == "Emergency" && p.Status == "Active")
                .GroupBy(p => p.BloodType)
                .Select(g => new { BloodType = g.Key, UrgencySum = g.Sum(x => x.DonorsNeeded) })
                .ToListAsync();

            var bloodDemandList = bloodTypes.Select(type => new
            {
                BloodType = type,
                Count = bloodDemand.FirstOrDefault(b => b.BloodType == type)?.UrgencySum ?? 0
            }).ToList();

            // 6. Attendance Rates
            var totalValidAppts = await _context.Appointments.CountAsync(a => a.Status != "Canceled");
            var attendedAppts = await _context.Appointments.CountAsync(a => a.Status == "CheckedIn" || a.Status == "Completed");
            var noShowAppts = await _context.Appointments.CountAsync(a => a.Status == "NoShow" || (a.Status == "Booked" && a.AppointmentDateTime < now));

            double attendanceRate = totalValidAppts > 0 ? ((double)attendedAppts / totalValidAppts) * 100 : 0;
            double noShowRate = totalValidAppts > 0 ? ((double)noShowAppts / totalValidAppts) * 100 : 0;

            return Ok(new
            {
                UsersTimeline = usersTimeline,
                AppointmentsTimeline = appointmentsTimeline,
                EmergencyPostsLastMonth = emergencyCount,
                BloodAvailability = bloodAvailabilityList,
                BloodDemand = bloodDemandList,
                AttendanceRate = Math.Round(attendanceRate, 1),
                NoShowRate = Math.Round(noShowRate, 1)
            });
        }

        // Additional admin route for getting users list with filters
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsersList([FromQuery] string? search, [FromQuery] string? eligibility, [FromQuery] string? bloodType)
        {
            var query = _context.Users.Where(u => u.Role == "User").AsQueryable();

            if (!string.IsNullOrEmpty(eligibility))
            {
                query = query.Where(u => u.EligibilityStatus == eligibility);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.FullName.Contains(search) || 
                                         u.NationalId.Contains(search) || 
                                         u.MobileNumber.Contains(search));
            }

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

            var userList = new List<object>();

            foreach (var user in users)
            {
                // Fetch latest donation form to get blood group info
                var form = await _context.DonationForms
                    .Where(f => f.UserId == user.Id)
                    .OrderByDescending(f => f.SubmissionDate)
                    .FirstOrDefaultAsync();

                var userBloodType = form != null ? form.BloodGroup + form.RhFactor : "Not screened";
                
                // If blood type filter is specified, check compatibility
                if (!string.IsNullOrEmpty(bloodType) && userBloodType != bloodType)
                {
                    continue;
                }

                userList.Add(new
                {
                    user.Id,
                    user.FullName,
                    user.NationalId,
                    user.MobileNumber,
                    user.Email,
                    user.DateOfBirth,
                    user.Gender,
                    user.Nationality,
                    user.EligibilityStatus,
                    user.EligibilityExpiryDate,
                    user.CreatedAt,
                    BloodType = userBloodType,
                    BloodGroup = form?.BloodGroup ?? "A",
                    RhFactor = form?.RhFactor ?? "+",
                    Weight = form?.Weight ?? 0,
                    Hemoglobin = form?.Hemoglobin ?? 0,
                    Hematocrit = form?.Hematocrit ?? 0,
                    LatestFormId = form?.Id,
                    DontKnowVitals = form != null && form.Hemoglobin == 0 && form.Hematocrit == 0
                });
            }

            return Ok(userList);
        }

        // Notifications management for admin & users
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);
            bool isAdmin = roleClaim?.Value == "Admin";

            List<Notification> notifications;

            if (isAdmin)
            {
                // Admins see notifications addressed to null (system alerts) and their own
                notifications = await _context.Notifications
                    .Where(n => n.UserId == null || n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(30)
                    .ToListAsync();
            }
            else
            {
                notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(30)
                    .ToListAsync();
            }

            return Ok(notifications);
        }

        [HttpPost("notifications/read")]
        public async Task<IActionResult> MarkNotificationsAsRead()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);
            bool isAdmin = roleClaim?.Value == "Admin";

            IQueryable<Notification> query = _context.Notifications.Where(n => !n.IsRead);

            if (isAdmin)
            {
                query = query.Where(n => n.UserId == null || n.UserId == userId);
            }
            else
            {
                query = query.Where(n => n.UserId == userId);
            }

            var unread = await query.ToListAsync();
            foreach (var n in unread)
            {
                n.IsRead = true;
            }

            _context.Notifications.UpdateRange(unread);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "All notifications marked as read." });
        }

        [HttpPost("user/{id}/edit")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUserProfile(int id, [FromBody] EditUserProfileDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { Message = "User not found." });

            // Update user details
            user.FullName = dto.FullName;
            user.NationalId = dto.NationalId;
            user.MobileNumber = dto.MobileNumber;
            user.Email = dto.Email;
            user.DateOfBirth = dto.DateOfBirth;
            user.Gender = dto.Gender;
            user.Nationality = dto.Nationality;
            user.EligibilityStatus = dto.EligibilityStatus;

            // Update or create the latest screening form vitals
            var form = await _context.DonationForms
                .Where(f => f.UserId == id)
                .OrderByDescending(f => f.SubmissionDate)
                .FirstOrDefaultAsync();

            int age = DateTime.UtcNow.Year;
            if (DateTime.TryParse(dto.DateOfBirth, out var dob))
            {
                age = DateTime.UtcNow.Year - dob.Year;
            }

            if (form == null)
            {
                form = new DonationForm
                {
                    UserId = id,
                    SubmissionDate = DateTime.UtcNow,
                    EligibilityQuestionsJson = "{}"
                };
                _context.DonationForms.Add(form);
            }

            form.Age = age;
            form.BloodGroup = dto.BloodGroup;
            form.RhFactor = dto.RhFactor;
            form.Weight = dto.Weight;
            form.Hemoglobin = dto.Hemoglobin;
            form.Hematocrit = dto.Hematocrit;
            form.EligibilityResult = dto.EligibilityStatus == "Eligible" ? "Eligible" : "Ineligible";
            form.AdminNotes = $"Updated by admin: {dto.AdminNotes}";

            _context.Users.Update(user);
            if (form.Id > 0)
            {
                _context.DonationForms.Update(form);
            }
            await _context.SaveChangesAsync();

            // Create notification
            _context.Notifications.Add(new Notification
            {
                UserId = id,
                Message = $"Your donor profile and medical parameters were updated by admin. Status: {dto.EligibilityStatus}.",
                Type = "Info"
            });
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Profile and vitals updated successfully." });
        }
    }

    public class EditUserProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string EligibilityStatus { get; set; } = string.Empty;
        
        // Vitals
        public string BloodGroup { get; set; } = string.Empty;
        public string RhFactor { get; set; } = string.Empty;
        public double Weight { get; set; }
        public double Hemoglobin { get; set; }
        public double Hematocrit { get; set; }
        public string AdminNotes { get; set; } = string.Empty;
    }
}
