using System.ComponentModel.DataAnnotations;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class Criteria
    {
        [Key]
        public int CriteriaId { get; set; }

        [Required]
        public string? Name { get; set; }

    }
}
