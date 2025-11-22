using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class ActivityLog
    {
        [Key]
        public int ActivityLogId { get; set; }

        // User information
        public int? StudentId { get; set; }
        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; set; }

        public int? UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [StringLength(50)]
        public string Role { get; set; } = string.Empty;

        // Activity details
        [Required]
        [StringLength(50)]
        public string ActivityType { get; set; } = string.Empty; // Login, Logout, EvaluationStarted, EvaluationCompleted

        [StringLength(500)]
        public string? Description { get; set; }

        // IP and Location
        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Region { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        // Timestamps
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // For Login/Logout tracking
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public TimeSpan? Duration { get; set; }

        // Evaluation specific
        public int? EvaluationId { get; set; }
        [ForeignKey(nameof(EvaluationId))]
        public Evaluation? Evaluation { get; set; }

        public int? TeacherId { get; set; }
        [ForeignKey(nameof(TeacherId))]
        public Teacher? Teacher { get; set; }

        public int? SubjectId { get; set; }
        [ForeignKey(nameof(SubjectId))]
        public Subject? Subject { get; set; }
    }
}
