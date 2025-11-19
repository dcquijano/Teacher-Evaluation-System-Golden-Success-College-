using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Teacher_Evaluation_System__Golden_Success_College_.ViewModels.StudentViewModel;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class Teacher
    {
        [Key]
        public int TeacherId { get; set; }

        [Required]
        public string? FullName { get; set; }
        [Required]
        public string? Department { get; set; }

        // Level the teacher teacher
        public int LevelId { get; set; }
        [ForeignKey(nameof(LevelId))]
        public Level? Level { get; set; } // HighSchool, SeniorHigh, College

        // NEW: Picture file path
        public string? PicturePath { get; set; }
        // Example saved value: "/images/teachers/teacher1.jpg"

        // Status Active or InActive For Teachers
        public bool IsActive { get; set; } = true;
        // Navigation: Evaluations for this teacher
        public ICollection<Evaluation>? Evaluations { get; set; }

    }
}
