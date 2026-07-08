using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodDonation.API.Data;
using BloodDonation.API.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BloodDonation.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PostController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveFeed([FromQuery] string? type, [FromQuery] string? bloodType, [FromQuery] string? search)
        {
            var query = _context.Posts.AsQueryable();

            // Filter out completed and archived, and expired emergency posts
            query = query.Where(p => p.Status == "Active");

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(p => p.Type == type);
            }

            if (!string.IsNullOrEmpty(bloodType))
            {
                query = query.Where(p => p.BloodType == bloodType || p.BloodType == "Any");
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Title.Contains(search) || 
                                         p.Description.Contains(search) || 
                                         p.Location.Contains(search));
            }

            var posts = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            
            // Clean up/filter expired posts on fetching (emergency posts where ExpiryTime < Now)
            var activePosts = posts.Where(p => p.Type != "Emergency" || p.ExpiryTime == null || p.ExpiryTime > DateTime.UtcNow).ToList();

            return Ok(activePosts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPostById(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound(new { Message = "Post not found." });
            return Ok(post);
        }

        // Admin Endpoints
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
        {
            var post = new Post
            {
                Type = dto.Type,
                Title = dto.Title,
                Description = dto.Description,
                Location = dto.Location,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,

                // Emergency fields
                BloodType = dto.BloodType ?? string.Empty,
                UrgencyLevel = dto.UrgencyLevel ?? "Medium",
                ContactInfo = dto.ContactInfo ?? string.Empty,
                DonorsNeeded = dto.DonorsNeeded,
                ExpiryTime = dto.ExpiryTime,

                // Event fields
                EventDate = dto.EventDate,
                StartDateTime = dto.StartDateTime,
                EndDateTime = dto.EndDateTime,
                MaxCapacity = dto.MaxCapacity,
                AvailableSlots = dto.MaxCapacity // Initially all slots are available
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // Notify users if it is a high urgency emergency post
            if (post.Type == "Emergency" && (post.UrgencyLevel == "High" || post.UrgencyLevel == "Emergency"))
            {
                // Create system-wide notifications for relevant users (or all users)
                var users = await _context.Users.Where(u => u.Role == "User").ToListAsync();
                foreach (var user in users)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = user.Id,
                        Message = $"URGENT EMERGENCY: {post.BloodType} needed at {post.Location}. Contact: {post.ContactInfo}.",
                        Type = "Alert"
                    });
                }
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetPostById), new { id = post.Id }, post);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] CreatePostDto dto)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound(new { Message = "Post not found." });

            post.Title = dto.Title;
            post.Description = dto.Description;
            post.Location = dto.Location;
            post.Status = dto.Status ?? post.Status;

            if (post.Type == "Emergency")
            {
                post.BloodType = dto.BloodType ?? post.BloodType;
                post.UrgencyLevel = dto.UrgencyLevel ?? post.UrgencyLevel;
                post.ContactInfo = dto.ContactInfo ?? post.ContactInfo;
                post.DonorsNeeded = dto.DonorsNeeded;
                post.ExpiryTime = dto.ExpiryTime;
            }
            else // Event
            {
                post.EventDate = dto.EventDate;
                post.StartDateTime = dto.StartDateTime;
                post.EndDateTime = dto.EndDateTime;
                
                // Adjust slots if max capacity changed
                if (dto.MaxCapacity != post.MaxCapacity)
                {
                    int bookedSlots = post.MaxCapacity - post.AvailableSlots;
                    post.MaxCapacity = dto.MaxCapacity;
                    post.AvailableSlots = Math.Max(0, dto.MaxCapacity - bookedSlots);
                }
            }

            _context.Posts.Update(post);
            await _context.SaveChangesAsync();

            return Ok(post);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound(new { Message = "Post not found." });

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Post deleted successfully." });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompletePost(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound(new { Message = "Post not found." });

            post.Status = "Completed";
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Post marked as completed." });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/archive")]
        public async Task<IActionResult> ArchivePost(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound(new { Message = "Post not found." });

            post.Status = "Archived";
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Post archived." });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}/analytics")]
        public async Task<IActionResult> GetEventAnalytics(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound(new { Message = "Post not found." });

            var appointments = await _context.Appointments
                .Include(a => a.User)
                .Where(a => a.PostId == id)
                .ToListAsync();

            int totalBookings = appointments.Count;
            int bookedCount = appointments.Count(a => a.Status == "Booked");
            int checkedInCount = appointments.Count(a => a.Status == "CheckedIn");
            int completedCount = appointments.Count(a => a.Status == "Completed");
            int canceledCount = appointments.Count(a => a.Status == "Canceled");
            int noShowCount = appointments.Count(a => a.Status == "NoShow");

            int activeBookings = totalBookings - canceledCount;
            double attendanceRate = activeBookings > 0 
                ? Math.Round(((double)(checkedInCount + completedCount) / activeBookings) * 100, 1) 
                : 0;

            // Blood type distribution & Vitals check counts
            var bloodTypeCounts = new Dictionary<string, int>();
            int vitalsCheckNeededCount = 0;
            var bookingsList = new List<object>();

            foreach (var appt in appointments)
            {
                if (appt.User == null) continue;

                var form = await _context.DonationForms
                    .Where(f => f.UserId == appt.UserId)
                    .OrderByDescending(f => f.SubmissionDate)
                    .FirstOrDefaultAsync();

                string bloodType = form != null ? $"{form.BloodGroup}{form.RhFactor}" : "Not screened";
                bool vitalsCheck = form != null && form.Hemoglobin == 0 && form.Hematocrit == 0;

                if (appt.Status != "Canceled")
                {
                    if (bloodTypeCounts.ContainsKey(bloodType))
                        bloodTypeCounts[bloodType]++;
                    else
                        bloodTypeCounts[bloodType] = 1;

                    if (vitalsCheck)
                    {
                        vitalsCheckNeededCount++;
                    }
                }

                bookingsList.Add(new
                {
                    appt.Id,
                    DonorName = appt.User.FullName,
                    DonorNationalId = appt.User.NationalId,
                    DonorPhone = appt.User.MobileNumber,
                    SlotTime = appt.AppointmentDateTime.ToString("yyyy-MM-dd HH:mm"),
                    appt.Status,
                    BloodType = bloodType,
                    VitalsCheckNeeded = vitalsCheck
                });
            }

            // Transform blood type dictionary to standard list structure
            var bloodTypeDistribution = bloodTypeCounts.Select(kvp => new { BloodType = kvp.Key, Count = kvp.Value }).ToList();

            return Ok(new
            {
                PostId = post.Id,
                Title = post.Title,
                Location = post.Location,
                StartDateTime = post.StartDateTime,
                EndDateTime = post.EndDateTime,
                TotalBookings = totalBookings,
                BookedCount = bookedCount,
                CheckedInCount = checkedInCount,
                CompletedCount = completedCount,
                CanceledCount = canceledCount,
                NoShowCount = noShowCount,
                AttendanceRate = attendanceRate,
                VitalsCheckNeededCount = vitalsCheckNeededCount,
                BloodTypeDistribution = bloodTypeDistribution,
                Bookings = bookingsList
            });
        }
    }

    public class CreatePostDto
    {
        public string Type { get; set; } = string.Empty; // "Emergency" or "Event"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Status { get; set; }

        // Emergency Post Specifics
        public string? BloodType { get; set; }
        public string? UrgencyLevel { get; set; }
        public string? ContactInfo { get; set; }
        public int DonorsNeeded { get; set; }
        public DateTime? ExpiryTime { get; set; }

        // Event Post Specifics
        public DateTime? EventDate { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public int MaxCapacity { get; set; }
    }
}
