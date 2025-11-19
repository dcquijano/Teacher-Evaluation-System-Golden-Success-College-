using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class Section
    {
        [Key]
        public int SectionId { get; set; }
        [Required]
        public string? SectionName { get; set; }

        public int LevelId { get; set; }

        [ForeignKey(nameof(LevelId))]
        public Level? Level { get; set; }

        public ICollection<Student>? Students { get; set; }
        public ICollection<Subject>? Subjects { get; set; }
    }
}
