using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Teacher_Evaluation_System__Golden_Success_College_.Services;
using Teacher_Evaluation_System__Golden_Success_College_.ViewModels;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    [Authorize(Roles = "Admin,Super Admin")]
    public class LogsController : Controller
    {
        private readonly IActivityLogService _activityLogService;

        public LogsController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }

        public async Task<IActionResult> Index(
       DateTime? filterDateFrom,
       DateTime? filterDateTo,
       string? filterActivityType,
       string? filterUsername,
       int page = 1)
        {
            const int pageSize = 20;

            var logs = await _activityLogService.GetAllActivityLogsAsync(
                filterDateFrom,
                filterDateTo,
                filterActivityType
            );

            // Username filter
            if (!string.IsNullOrWhiteSpace(filterUsername))
            {
                logs = logs.Where(l => l.Username.Contains(filterUsername, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Pagination
            var totalCount = logs.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            logs = logs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new ActivityLogListViewModel
            {
                ActivityLogs = logs.Select(log => new ActivityLogItemViewModel
                {
                    ActivityLogId = log.ActivityLogId,
                    Username = log.Username,
                    Role = log.Role,
                    ActivityType = log.ActivityType,
                    Description = log.Description,
                    IpAddress = log.IpAddress,
                    Location = $"{log.City}, {log.Region}, {log.Country}",
                    TimeIn = log.TimeIn,
                    TimeOut = log.TimeOut,
                    Duration = log.Duration,
                    Timestamp = log.Timestamp,
                    TeacherName = log.Teacher?.FullName,
                    SubjectName = log.Subject != null ? $"{log.Subject.SubjectCode} - {log.Subject.SubjectName}" : null
                }).ToList(),

                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,

                FilterDateFrom = filterDateFrom,
                FilterDateTo = filterDateTo,
                FilterActivityType = filterActivityType,
                FilterUsername = filterUsername
            };

            // Dropdown
            ViewBag.ActivityTypes = new SelectList(new[]
            {
        new { Value = "", Text = "All Activities" },
        new { Value = "Login", Text = "Login" },
        new { Value = "Logout", Text = "Logout" },
        new { Value = "EvaluationStarted", Text = "Evaluation Started" },
        new { Value = "EvaluationCompleted", Text = "Evaluation Completed" }
    }, "Value", "Text", filterActivityType);

            return View(viewModel);
        }



        // GET: Logs/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var logs = await _activityLogService.GetAllActivityLogsAsync(null, null, null);
            var log = logs.FirstOrDefault(l => l.ActivityLogId == id);

            if (log == null)
            {
                return NotFound();
            }

            var viewModel = new ActivityLogItemViewModel
            {
                ActivityLogId = log.ActivityLogId,
                Username = log.Username,
                Role = log.Role,
                ActivityType = log.ActivityType,
                Description = log.Description,
                IpAddress = log.IpAddress,
                Location = $"{log.City}, {log.Region}, {log.Country}",
                City = log.City,
                Region = log.Region,
                Country = log.Country,
                UserAgent = log.UserAgent,
                TimeIn = log.TimeIn,
                TimeOut = log.TimeOut,
                Duration = log.Duration,
                Timestamp = log.Timestamp,
                TeacherName = log.Teacher?.FullName,
                SubjectName = log.Subject != null ? $"{log.Subject.SubjectCode} - {log.Subject.SubjectName}" : null,
                EvaluationId = log.EvaluationId
            };

            return View(viewModel);
        }
    }
}