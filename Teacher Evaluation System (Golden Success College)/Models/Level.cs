using System.ComponentModel.DataAnnotations;
using static System.Collections.Specialized.BitVector32;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class Level
    {
        [Key]
        public int LevelId { get; set; }

        [Required]
        public string? LevelName { get; set; } // JHS, SHS, College

        [Required]
        public string? Description { get; set; }  // Details about the level


        public ICollection<Section>? Sections { get; set; }
    }
}
