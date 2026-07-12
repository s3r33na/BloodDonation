using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodDonation.API.Data;
using BloodDonation.API.Helpers;
using BloodDonation.API.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BloodDonation.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { Message = "Email and Password are required." });
            }

            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
            {
                return BadRequest(new { Message = "Email is already registered." });
            }

            // Validate Jordanian National ID
            if (dto.Nationality.Equals("Jordanian", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(dto.NationalId) || !Regex.IsMatch(dto.NationalId, @"^\d{10}$"))
                {
                    return BadRequest(new { Message = "Jordanian National ID must be exactly 10 digits." });
                }

                // Check if national ID already registered
                if (await _context.Users.AnyAsync(u => u.NationalId == dto.NationalId))
                {
                    return BadRequest(new { Message = "National ID is already registered." });
                }
            }
            else
            {
                // Eligibility gate: Non-Jordanian users are permanently ineligible to donate
                // (or blocked from booking)
            }

            // Check age
            int age = 0;
            if (DateTime.TryParse(dto.DateOfBirth, out var dob))
            {
                age = DateTime.UtcNow.Year - dob.Year;
                if (dob.Date > DateTime.UtcNow.AddYears(-age)) age--;
            }

            // Determine initial eligibility status
            string initialStatus = "PendingReview";
            if (!dto.Nationality.Equals("Jordanian", StringComparison.OrdinalIgnoreCase))
            {
                initialStatus = "PermanentlyNotEligible"; // Organization rule gate
            }
            else if (age < 18 || age > 65)
            {
                initialStatus = "TemporarilyNotEligible"; // Age limit gate
            }

            var user = new User
            {
                FullName = dto.FullName,
                NationalId = dto.NationalId,
                MobileNumber = dto.MobileNumber,
                Email = dto.Email,
                PasswordHash = SecurityHelper.HashPassword(dto.Password),
                DateOfBirth = dto.DateOfBirth,
                BloodType = dto.BloodType,
                Gender = dto.Gender,
                Nationality = dto.Nationality,
                Role = "User", // Default registered users are donors
                EligibilityStatus = initialStatus,
                EligibilityExpiryDate = initialStatus == "TemporarilyNotEligible" ? DateTime.UtcNow.AddMonths(6) : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create notification for admin and user
            _context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Message = $"Welcome to Luminus Giving Initiative! Your account status is: {user.EligibilityStatus}.",
                Type = "Info"
            });
            
            // Notification for Admins (UserId = null)
            _context.Notifications.Add(new Notification
            {
                UserId = null,
                Message = $"New donor registered: {user.FullName} ({user.Nationality}).",
                Type = "Alert"
            });

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Registration successful. Please log in." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (user == null || !SecurityHelper.VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Unauthorized(new { Message = "Invalid email or password." });
            }

            var secretKey = _configuration["Jwt:Key"] ?? "SuperSecretKeyForBloodDonationSystem2026!";
            var token = SecurityHelper.GenerateJwtToken(user, secretKey);

            return Ok(new
            {
                Token = token,
                User = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.EligibilityStatus,
                    user.NationalId,
                    user.Nationality,
                    user.MobileNumber,
                    user.DateOfBirth,
                    user.BloodType,
                    user.Gender
                }
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.EligibilityStatus,
                user.NationalId,
                user.Nationality,
                user.MobileNumber,
                user.DateOfBirth,
                user.BloodType,
                user.Gender
            });
        }
    }

    public class RegisterDto
    {
        public string FullName { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
