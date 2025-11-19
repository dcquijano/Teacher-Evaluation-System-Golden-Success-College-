using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Teacher_Evaluation_System__Golden_Success_College_.ViewModels
{
    // Main ViewModel for the evaluation form (like Image 1)
    public class EvaluationFormViewModel
    {
        public int TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public string? TeacherPicturePath { get; set; }
        public string? TeacherDepartment { get; set; }

        public int SubjectId { get; set; }
        public IEnumerable<SelectListItem> Subjects { get; set; } = new List<SelectListItem>();

        public int StudentId { get; set; }
        public string? StudentName { get; set; }

        public bool IsAnonymous { get; set; }

        [MaxLength(1000)]
        public string? Comments { get; set; }

        public DateTime DateEvaluated { get; set; } = DateTime.Now;

        // List of criteria with their questions
        public List<CriteriaWithQuestionsViewModel> CriteriaGroups { get; set; } = new();

        // Rating scale information
        public string RatingScaleDescription { get; set; } =
            "5 - Frequently observed / collaboratively demonstrated\n" +
            "4 - Occasionally observed / collaboratively demonstrated\n" +
            "3 - Sometimes observed / publicly demonstrated\n" +
            "2 - Rarely observed / collaboratively demonstrated\n" +
            "1 - Not observed at all";
    }

    // Criteria section with its questions
    public class CriteriaWithQuestionsViewModel
    {
        public int CriteriaId { get; set; }
        public string? CriteriaName { get; set; }

        // Questions under this criteria
        public List<QuestionResponseViewModel> Questions { get; set; } = new();

        // Average score for this criteria section
        public double CriteriaAverage { get; set; }
    }

    // Individual question with its score
    public class QuestionResponseViewModel
    {
        public int QuestionId { get; set; }
        public string? Description { get; set; }

        [Range(1, 5, ErrorMessage = "Score must be between 1 and 5")]
        public int ScoreValue { get; set; }
    }

    // ViewModel for submitting the evaluation
    public class SubmitEvaluationViewModel
    {
        [Required]
        public int TeacherId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [Required]
        public int StudentId { get; set; }

        public bool IsAnonymous { get; set; }

        [MaxLength(1000)]
        public string? Comments { get; set; }

        // List of question scores
        [Required]
        [MinLength(1, ErrorMessage = "At least one question must be answered")]
        public List<QuestionScoreDto> QuestionScores { get; set; } = new();
    }

    public class QuestionScoreDto
    {
        [Required]
        public int QuestionId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Score must be between 1 and 5")]
        public int ScoreValue { get; set; }
    }

    // ViewModel for displaying evaluation results (for viewing completed evaluations)
    public class EvaluationResultViewModel
    {
        public int EvaluationId { get; set; }
        public string? TeacherName { get; set; }
        public string? TeacherPicturePath { get; set; }
        public string? TeacherDepartment { get; set; }
        public string? SubjectName { get; set; }
        public string? StudentName { get; set; }
        public bool IsAnonymous { get; set; }
        public DateTime DateEvaluated { get; set; }
        public string? Comments { get; set; }
        public double OverallAverage { get; set; }

        public List<CriteriaResultViewModel> CriteriaResults { get; set; } = new();
    }

    public class CriteriaResultViewModel
    {
        public string? CriteriaName { get; set; }
        public List<QuestionResultViewModel> Questions { get; set; } = new();
        public double CriteriaAverage { get; set; }
    }

    public class QuestionResultViewModel
    {
        public string? Description { get; set; }
        public int ScoreValue { get; set; }
    }

    // ViewModel for teacher evaluation summary (aggregated results)
    public class TeacherEvaluationSummaryViewModel
    {
        public int TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public string? SubjectName { get; set; }

        public int TotalEvaluations { get; set; }
        public double OverallAverage { get; set; }

        public List<CriteriaSummaryViewModel> CriteriaSummaries { get; set; } = new();

        public List<string> RecentComments { get; set; } = new();
    }

    public class CriteriaSummaryViewModel
    {
        public string? CriteriaName { get; set; }
        public List<QuestionSummaryViewModel> QuestionSummaries { get; set; } = new();
        public double CriteriaAverage { get; set; }
    }

    public class QuestionSummaryViewModel
    {
        public string? Description { get; set; }
        public double AverageScore { get; set; }
        public int ResponseCount { get; set; }

        // Distribution of scores (how many 1s, 2s, 3s, 4s, 5s)
        public Dictionary<int, int> ScoreDistribution { get; set; } = new();
    }

    // ViewModel for the table display (like Images 2-5)
    public class EvaluationListViewModel
    {
        public List<EvaluationListItemViewModel> Evaluations { get; set; } = new();

        // Filters
        public string? FilterTeacherName { get; set; }
        public string? FilterSubjectName { get; set; }
        public DateTime? FilterDateFrom { get; set; }
        public DateTime? FilterDateTo { get; set; }
    }

    public class EvaluationListItemViewModel
    {
        public int EvaluationId { get; set; }
        public string? SubjectName { get; set; }
        public string? TeacherName { get; set; }
        public string? TeacherPicturePath { get; set; }
        public string? StudentName { get; set; }
        public bool IsAnonymous { get; set; }
        public DateTime DateEvaluated { get; set; }
        public string? Comments { get; set; }
        public double AverageScore { get; set; }
    }

    // ViewModel for criteria management (Images 2-3)
    public class CriteriaManagementViewModel
    {
        public List<CriteriaWithQuestionsListViewModel> Criteria { get; set; } = new();
    }

    public class CriteriaWithQuestionsListViewModel
    {
        public int CriteriaId { get; set; }
        public string? Name { get; set; }
        public List<QuestionListViewModel> Questions { get; set; } = new();
    }

    public class QuestionListViewModel
    {
        public int QuestionId { get; set; }
        public string? Description { get; set; }
    }
}