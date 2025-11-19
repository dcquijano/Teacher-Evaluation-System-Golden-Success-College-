using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class Question
    {
        [Key]
        public int QuestionId { get; set; }


        // Foreign key to Criteria
        public int CriteriaId { get; set; }
        [ForeignKey(nameof(CriteriaId))]
        public Criteria? Criteria { get; set; }

        [Required]
        public string? Description { get; set; }


  

    }
}
