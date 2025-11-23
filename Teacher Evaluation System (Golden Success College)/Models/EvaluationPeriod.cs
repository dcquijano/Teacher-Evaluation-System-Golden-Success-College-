using System.ComponentModel.DataAnnotations;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class EvaluationPeriod
    {
        [Key]
        public int EvaluationPeriodId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Period Name")]
        public string PeriodName { get; set; } // e.g., "First Semester 2024-2025"

        [Required]
        [Display(Name = "Academic Year")]
        [StringLength(20)]
        public string AcademicYear { get; set; } // e.g., "2024-2025"

        [Required]
        [Display(Name = "Semester/Term")]
        public string Semester { get; set; } // e.g., "First Semester", "Second Semester", "Summer"

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Current Period")]
        public bool IsCurrent { get; set; } = false;

        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Created By")]
        public string? CreatedBy { get; set; }

        // Navigation property
        public virtual ICollection<Evaluation>? Evaluations { get; set; }

        // Computed property
        [Display(Name = "Status")]
        public string Status
        {
            get
            {
                var today = DateTime.Today;
                if (today < StartDate) return "Upcoming";
                if (today > EndDate) return "Completed";
                return "Active";
            }
        }

        // Check if period is valid for evaluation
        public bool IsValidForEvaluation()
        {
            var today = DateTime.Today;
            return IsActive && today >= StartDate && today <= EndDate;
        }
    }
}