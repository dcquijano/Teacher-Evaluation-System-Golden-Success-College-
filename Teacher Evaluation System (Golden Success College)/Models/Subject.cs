using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class Subject
    {
        [Key]
        public int SubjectId { get; set; }

        [Required]
        public string? SubjectName { get; set; }

        [MaxLength(20)]
        [Required]
        public string? SubjectCode { get; set; } // e.g., "CS101"
        public int SectionId { get; set; }
        [ForeignKey(nameof(SectionId))]
        public Section? Section { get; set; }

        public int TeacherId { get; set; }
        [ForeignKey(nameof(TeacherId))]
        public Teacher? Teacher { get; set; }

        public string? Schedule { get; set; } // e.g., "MWF 10:00-11:30"

        public ICollection<Evaluation>? Evaluations { get; set; }
    }
}
