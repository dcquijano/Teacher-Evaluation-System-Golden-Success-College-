using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Teacher_Evaluation_System__Golden_Success_College_.ViewModels.StudentViewModel;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class Teacher
    {
        [Key]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department is required")]
        [StringLength(50, ErrorMessage = "Department cannot exceed 50 characters")]
        public string Department { get; set; } = string.Empty;

        // Level the teacher teaches
        [Required(ErrorMessage = "Level is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid level")]
        public int LevelId { get; set; }

        [ForeignKey(nameof(LevelId))]
        public Level? Level { get; set; }

        // Picture file path (optional)
        [StringLength(255)]
        public string? PicturePath { get; set; }

        // Status Active or InActive For Teachers
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<Evaluation>? Evaluations { get; set; }
        public ICollection<Enrollment>? Enrollments { get; set; }

    }
}
