using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Teacher_Evaluation_System__Golden_Success_College_.ViewModels.StudentViewModel;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        public string? FullName { get; set; }
        public string? Email { get; set; }

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        // Junior High | Senior High | College
        public int LevelId { get; set; }

        [ForeignKey(nameof(LevelId))]
        public Level? Level { get; set; }

        // JHS & SHS use Section
        public int? SectionId { get; set; }  // COLLEGE = NULL

        [ForeignKey(nameof(SectionId))]
        public Section? Section { get; set; }

        // COLLEGE ONLY → 1st Year, 2nd Year, etc.
        public int? CollegeYearLevel { get; set; }

        // Role: Student, Admin, SuperAdmin
        public int RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        public Role? Role { get; set; }
    }
}
