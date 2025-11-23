using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class Evaluation
    {
        [Key]
        public int EvaluationId { get; set; }

        // FIXED: Added SubjectId to know which class was evaluated

        [Display(Name = "Evaluation Period")]
        public int? EvaluationPeriodId { get; set; }

        [ForeignKey("EvaluationPeriodId")]
        public virtual EvaluationPeriod? EvaluationPeriod { get; set; }


        public int SubjectId { get; set; }
        [ForeignKey(nameof(SubjectId))]
        public Subject? Subject { get; set; }

        public int TeacherId { get; set; }
        [ForeignKey(nameof(TeacherId))]
        public Teacher? Teacher { get; set; }

        public int StudentId { get; set; }
        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; set; }

        public bool IsAnonymous { get; set; }

        public DateTime DateEvaluated { get; set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        public string? Comments { get; set; } // Optional written feedback

        // Navigation
        public ICollection<Score>? Scores { get; set; }


        // COMPUTED: Average score for this evaluation
        [NotMapped]
        public double AverageScore => Scores?.Any() == true
            ? Scores.Average(s => s.ScoreValue)
            : 0;
    }
}
