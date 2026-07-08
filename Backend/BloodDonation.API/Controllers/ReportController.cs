using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodDonation.API.Data;
using BloodDonation.API.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodDonation.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportData([FromQuery] string type, [FromQuery] string format)
        {
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(format))
            {
                return BadRequest("Parameters 'type' and 'format' are required.");
            }

            string[] headers;
            List<string[]> rows = new List<string[]>();
            string title = "";

            if (type.Equals("users", StringComparison.OrdinalIgnoreCase))
            {
                title = "Registered Donors List";
                headers = new[] { "ID", "Full Name", "National ID", "Email", "Phone", "DOB", "Gender", "Nationality", "Eligibility" };
                
                var users = await _context.Users.Where(u => u.Role == "User").ToListAsync();
                foreach (var u in users)
                {
                    rows.Add(new[]
                    {
                        u.Id.ToString(),
                        u.FullName,
                        u.NationalId,
                        u.Email,
                        u.MobileNumber,
                        u.DateOfBirth,
                        u.Gender,
                        u.Nationality,
                        u.EligibilityStatus
                    });
                }
            }
            else if (type.Equals("appointments", StringComparison.OrdinalIgnoreCase))
            {
                title = "Booked Appointments Report";
                headers = new[] { "ID", "Donor Name", "National ID", "Event Name", "Date/Time", "Status", "QR Code Token" };
                
                var appts = await _context.Appointments
                    .Include(a => a.User)
                    .Include(a => a.Post)
                    .ToListAsync();
                foreach (var a in appts)
                {
                    rows.Add(new[]
                    {
                        a.Id.ToString(),
                        a.User?.FullName ?? "Unknown",
                        a.User?.NationalId ?? "N/A",
                        a.Post?.Title ?? "N/A",
                        a.AppointmentDateTime.ToString("yyyy-MM-dd HH:mm"),
                        a.Status,
                        a.QrCodeToken
                    });
                }
            }
            else if (type.Equals("donations", StringComparison.OrdinalIgnoreCase))
            {
                title = "Completed Blood Donations Registry";
                headers = new[] { "ID", "Donor Name", "National ID", "Blood Type", "Rh", "Hemoglobin", "Hematocrit", "Date" };
                
                // Completed appointments are those marked as CheckedIn/Completed and status in attendance is Completed
                var completions = await _context.Attendances
                    .Include(at => at.Appointment)
                    .ThenInclude(a => a.User)
                    .Where(at => at.Status == "Completed")
                    .ToListAsync();

                foreach (var c in completions)
                {
                    var form = await _context.DonationForms
                        .Where(f => f.UserId == c.Appointment.UserId)
                        .OrderByDescending(f => f.SubmissionDate)
                        .FirstOrDefaultAsync();

                    rows.Add(new[]
                    {
                        c.Id.ToString(),
                        c.Appointment?.User?.FullName ?? "Unknown",
                        c.Appointment?.User?.NationalId ?? "N/A",
                        form?.BloodGroup ?? "N/A",
                        form?.RhFactor ?? "",
                        form?.Hemoglobin.ToString("F1") ?? "N/A",
                        form?.Hematocrit.ToString("F0") ?? "N/A",
                        c.CheckInTime.ToString("yyyy-MM-dd HH:mm")
                    });
                }
            }
            else if (type.Equals("attendance", StringComparison.OrdinalIgnoreCase))
            {
                title = "Event Attendance & Check-In Log";
                headers = new[] { "Check-In ID", "Name", "National ID", "Event Name", "Check-In Time", "Donation Status", "Verified By Admin" };
                
                var attendances = await _context.Attendances
                    .Include(at => at.Appointment)
                    .ThenInclude(a => a.User)
                    .Include(at => at.Appointment.Post)
                    .ToListAsync();

                foreach (var at in attendances)
                {
                    rows.Add(new[]
                    {
                        at.Id.ToString(),
                        at.Appointment?.User?.FullName ?? "Unknown",
                        at.Appointment?.User?.NationalId ?? "N/A",
                        at.Appointment?.Post?.Title ?? "N/A",
                        at.CheckInTime.ToString("yyyy-MM-dd HH:mm"),
                        at.Status,
                        at.VerifiedByAdminId
                    });
                }
            }
            else
            {
                return BadRequest("Invalid export type.");
            }

            // Export format parsing
            if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                var csvBytes = GenerateCsv(headers, rows);
                return File(csvBytes, "text/csv", $"{type}_report_{DateTime.Now:yyyyMMdd}.csv");
            }
            else if (format.Equals("excel", StringComparison.OrdinalIgnoreCase))
            {
                var excelBytes = GenerateExcelHtml(title, headers, rows);
                return File(excelBytes, "application/vnd.ms-excel", $"{type}_report_{DateTime.Now:yyyyMMdd}.xls");
            }
            else if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                var pdfBytes = PdfGenerator.GenerateTablePdf(title, headers, rows);
                return File(pdfBytes, "application/pdf", $"{type}_report_{DateTime.Now:yyyyMMdd}.pdf");
            }

            return BadRequest("Unsupported export format.");
        }

        private byte[] GenerateCsv(string[] headers, List<string[]> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
            }
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private string EscapeCsv(string val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            if (val.Contains(",") || val.Contains("\"") || val.Contains("\n") || val.Contains("\r"))
            {
                return $"\"{val.Replace("\"", "\"\"")}\"";
            }
            return val;
        }

        private byte[] GenerateExcelHtml(string title, string[] headers, List<string[]> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\">");
            sb.AppendLine("<head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"><style>");
            sb.AppendLine("table { border-collapse: collapse; font-family: Arial; }");
            sb.AppendLine("th { background-color: #D32F2F; color: #FFFFFF; font-weight: bold; padding: 6px; border: 1px solid #CCCCCC; }");
            sb.AppendLine("td { padding: 6px; border: 1px solid #CCCCCC; }");
            sb.AppendLine(".title { font-size: 16pt; font-weight: bold; color: #D32F2F; margin-bottom: 10px; }");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine($"<div class=\"title\">{title}</div>");
            sb.AppendLine($"<div>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</div><br/>");
            sb.AppendLine("<table><thead><tr>");
            
            foreach (var h in headers)
            {
                sb.AppendLine($"<th>{h}</th>");
            }
            sb.AppendLine("</tr></thead><tbody>");
            
            int count = 0;
            foreach (var r in rows)
            {
                var bg = count % 2 == 0 ? "#FFFFFF" : "#F9F9F9";
                sb.AppendLine($"<tr style=\"background-color: {bg};\">");
                foreach (var cell in r)
                {
                    sb.AppendLine($"<td>{cell}</td>");
                }
                sb.AppendLine("</tr>");
                count++;
            }
            sb.AppendLine("</tbody></table></body></html>");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        [HttpGet("export/event/{eventId}")]
        public async Task<IActionResult> ExportEventData(int eventId, [FromQuery] string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                return BadRequest("Parameter 'format' is required.");
            }

            var post = await _context.Posts.FindAsync(eventId);
            if (post == null) return NotFound(new { Message = "Post/Event not found." });

            var appts = await _context.Appointments
                .Include(a => a.User)
                .Where(a => a.PostId == eventId)
                .ToListAsync();

            string title = $"Event Bookings Report - {post.Title}";
            string[] headers = new[] { "Appt ID", "Donor Name", "National ID", "Email", "Phone", "Slot Time", "Status", "Blood Group" };
            var rows = new List<string[]>();

            foreach (var a in appts)
            {
                if (a.User == null) continue;

                var form = await _context.DonationForms
                    .Where(f => f.UserId == a.UserId)
                    .OrderByDescending(f => f.SubmissionDate)
                    .FirstOrDefaultAsync();

                string bloodGroup = form != null ? $"{form.BloodGroup}{form.RhFactor}" : "Not screened";

                rows.Add(new[]
                {
                    a.Id.ToString(),
                    a.User.FullName,
                    a.User.NationalId,
                    a.User.Email,
                    a.User.MobileNumber ?? "N/A",
                    a.AppointmentDateTime.ToString("yyyy-MM-dd HH:mm"),
                    a.Status,
                    bloodGroup
                });
            }

            if (format.Equals("excel", StringComparison.OrdinalIgnoreCase))
            {
                var excelBytes = GenerateExcelHtml(title, headers, rows);
                return File(excelBytes, "application/vnd.ms-excel", $"event_{eventId}_bookings_{DateTime.Now:yyyyMMdd}.xls");
            }
            else if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                var pdfBytes = PdfGenerator.GenerateTablePdf(title, headers, rows);
                return File(pdfBytes, "application/pdf", $"event_{eventId}_bookings_{DateTime.Now:yyyyMMdd}.pdf");
            }

            return BadRequest("Unsupported export format.");
        }
    }
}
