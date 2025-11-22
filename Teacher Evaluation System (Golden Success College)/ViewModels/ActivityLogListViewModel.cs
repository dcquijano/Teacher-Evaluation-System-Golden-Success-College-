namespace Teacher_Evaluation_System__Golden_Success_College_.ViewModels
{
    public class ActivityLogListViewModel
    {
        public List<ActivityLogItemViewModel> ActivityLogs { get; set; } = new();
        public DateTime? FilterDateFrom { get; set; }
        public DateTime? FilterDateTo { get; set; }
        public string? FilterActivityType { get; set; }
        public string? FilterUsername { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10; 
    }

    public class ActivityLogItemViewModel
    {
        public int ActivityLogId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IpAddress { get; set; }
        public string? Location { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? Country { get; set; }
        public string? UserAgent { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public TimeSpan? Duration { get; set; }
        public DateTime Timestamp { get; set; }
        public string? TeacherName { get; set; }
        public string? SubjectName { get; set; }
        public int? EvaluationId { get; set; }

        public string GetActivityBadgeClass()
        {
            return ActivityType switch
            {
                "Login" => "badge bg-success",
                "Logout" => "badge bg-secondary",
                "EvaluationStarted" => "badge bg-primary",
                "EvaluationCompleted" => "badge bg-info",
                _ => "badge bg-light text-dark"
            };
        }

        public string GetActivityIcon()
        {
            return ActivityType switch
            {
                "Login" => "bx bx-log-in",
                "Logout" => "bx bx-log-out",
                "EvaluationStarted" => "bx bx-edit",
                "EvaluationCompleted" => "bx bx-check-circle",
                _ => "bx bx-info-circle"
            };
        }

        public string FormatDuration()
        {
            if (Duration == null) return "N/A";

            var duration = Duration.Value;
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            if (duration.TotalMinutes >= 1)
                return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
            return $"{duration.Seconds}s";
        }
    }
}
