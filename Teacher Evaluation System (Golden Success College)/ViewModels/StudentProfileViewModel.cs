using System.ComponentModel.DataAnnotations;

namespace Teacher_Evaluation_System__Golden_Success_College_.ViewModels
{
    public class StudentProfileViewModel
    {
        public int StudentId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        public int LevelId { get; set; }
        public int? SectionId { get; set; }
        public int? CollegeYearLevel { get; set; }
    }
}
