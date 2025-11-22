using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;

namespace Teacher_Evaluation_System__Golden_Success_College_.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogService(
            Teacher_Evaluation_System__Golden_Success_College_Context context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogLoginAsync(ClaimsPrincipal user, string ipAddress, string userAgent)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = user.FindFirstValue(ClaimTypes.Name);
            var role = user.FindFirstValue(ClaimTypes.Role);
            var isStudent = user.IsInRole("Student");

            // Safety check - if userId is null, we can't log properly
            if (string.IsNullOrEmpty(userId))
            {
                // Log error or return early
                return;
            }

            var location = await GetLocationFromIpAsync(ipAddress);

            var log = new ActivityLog
            {
                StudentId = isStudent ? int.Parse(userId) : null,
                UserId = !isStudent ? int.Parse(userId) : null,
                Username = username ?? "Unknown",
                Role = role ?? "Unknown",
                ActivityType = "Login",
                Description = $"{username ?? "Unknown"} logged in",
                IpAddress = ipAddress,
                City = location.City,
                Region = location.Region,
                Country = location.Country,
                UserAgent = userAgent,
                TimeIn = DateTime.Now,
                Timestamp = DateTime.Now
            };

            _context.ActivityLog.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogLogoutAsync(ClaimsPrincipal user, string ipAddress)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = user.FindFirstValue(ClaimTypes.Name);
            var role = user.FindFirstValue(ClaimTypes.Role);
            var isStudent = user.IsInRole("Student");

            // Safety check
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
            {
                return;
            }

            // Find the last login entry for this user
            var lastLogin = await _context.ActivityLog
                .Where(a => a.Username == username && a.ActivityType == "Login" && a.TimeOut == null)
                .OrderByDescending(a => a.TimeIn)
                .FirstOrDefaultAsync();

            if (lastLogin != null)
            {
                lastLogin.TimeOut = DateTime.Now;
                lastLogin.Duration = lastLogin.TimeOut - lastLogin.TimeIn;
                _context.ActivityLog.Update(lastLogin);
            }

            // Also create a separate logout entry
            var log = new ActivityLog
            {
                StudentId = isStudent && int.TryParse(userId, out int studentId) ? studentId : null,
                UserId = !isStudent && int.TryParse(userId, out int userId2) ? userId2 : null,
                Username = username ?? "Unknown",
                Role = role ?? "Unknown",
                ActivityType = "Logout",
                Description = $"{username ?? "Unknown"} logged out",
                IpAddress = ipAddress,
                TimeOut = DateTime.Now,
                Timestamp = DateTime.Now
            };

            _context.ActivityLog.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogEvaluationStartedAsync(int studentId, int teacherId, int subjectId, string ipAddress)
        {
            var student = await _context.Student.FindAsync(studentId);
            var teacher = await _context.Teacher.FindAsync(teacherId);
            var subject = await _context.Subject.FindAsync(subjectId);

            var location = await GetLocationFromIpAsync(ipAddress);

            var log = new ActivityLog
            {
                StudentId = studentId,
                Username = student?.FullName ?? "Unknown",
                Role = "Student",
                ActivityType = "EvaluationStarted",
                Description = $"{student?.FullName ?? "Unknown"} started evaluating {teacher?.FullName ?? "Unknown"} for {subject?.SubjectCode ?? "Unknown"}",
                IpAddress = ipAddress,
                City = location.City,
                Region = location.Region,
                Country = location.Country,
                TeacherId = teacherId,
                SubjectId = subjectId,
                Timestamp = DateTime.Now
            };

            _context.ActivityLog.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogEvaluationCompletedAsync(int studentId, int evaluationId, int teacherId, int subjectId, string ipAddress)
        {
            var student = await _context.Student.FindAsync(studentId);
            var teacher = await _context.Teacher.FindAsync(teacherId);
            var subject = await _context.Subject.FindAsync(subjectId);

            var location = await GetLocationFromIpAsync(ipAddress);

            var log = new ActivityLog
            {
                StudentId = studentId,
                Username = student?.FullName ?? "Unknown",
                Role = "Student",
                ActivityType = "EvaluationCompleted",
                Description = $"{student?.FullName ?? "Unknown"} completed evaluating {teacher?.FullName ?? "Unknown"} for {subject?.SubjectCode ?? "Unknown"}",
                IpAddress = ipAddress,
                City = location.City,
                Region = location.Region,
                Country = location.Country,
                EvaluationId = evaluationId,
                TeacherId = teacherId,
                SubjectId = subjectId,
                Timestamp = DateTime.Now
            };

            _context.ActivityLog.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ActivityLog>> GetUserActivityLogsAsync(int? studentId, int? userId)
        {
            var query = _context.ActivityLog
                .Include(a => a.Student)
                .Include(a => a.User)
                .Include(a => a.Teacher)
                .Include(a => a.Subject)
                .Include(a => a.Evaluation)
                .AsQueryable();

            if (studentId.HasValue)
                query = query.Where(a => a.StudentId == studentId);

            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId);

            return await query
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

        public async Task<List<ActivityLog>> GetAllActivityLogsAsync(DateTime? fromDate, DateTime? toDate, string? activityType)
        {
            var query = _context.ActivityLog
                .Include(a => a.Student)
                .Include(a => a.User)
                .Include(a => a.Teacher)
                .Include(a => a.Subject)
                .Include(a => a.Evaluation)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(a => a.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.Timestamp <= toDate.Value.AddDays(1).AddTicks(-1));

            if (!string.IsNullOrEmpty(activityType))
                query = query.Where(a => a.ActivityType == activityType);

            return await query
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

        private async Task<(string City, string Region, string Country)> GetLocationFromIpAsync(string ipAddress)
        {
            // For localhost/development
            if (ipAddress == "::1" || ipAddress == "127.0.0.1" || string.IsNullOrEmpty(ipAddress))
            {
                return ("Local", "Local", "Local");
            }

            try
            {
                // Using ip-api.com free API (no key required, 45 requests/minute)
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var response = await client.GetStringAsync($"http://ip-api.com/json/{ipAddress}");

                var json = System.Text.Json.JsonDocument.Parse(response);
                var root = json.RootElement;

                if (root.GetProperty("status").GetString() == "success")
                {
                    return (
                        root.GetProperty("city").GetString() ?? "Unknown",
                        root.GetProperty("regionName").GetString() ?? "Unknown",
                        root.GetProperty("country").GetString() ?? "Unknown"
                    );
                }
            }
            catch
            {
                // If API fails, return Unknown
            }

            return ("Unknown", "Unknown", "Unknown");
        }
    }
}