using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class Score
    {
        [Key]
        public int ScoreId { get; set; }

        public int EvaluationId { get; set; }
        [ForeignKey(nameof(EvaluationId))]
        public Evaluation? Evaluation { get; set; }

        public int QuestionId { get; set; }
        [ForeignKey(nameof(QuestionId))]
        public Question? Question { get; set; }

        [Range(1, 5)] // Likert scale: 1=Poor, 5=Excellent
        public int ScoreValue { get; set; }
    }
}
