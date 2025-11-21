using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Teacher_Evaluation_System__Golden_Success_College_.ViewModels
{
    // Main ViewModel for the evaluation form
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

        public bool IsAnonymous { get; set; } = true;

        [MaxLength(1000)]
        public string? Comments { get; set; }

        public DateTime DateEvaluated { get; set; } = DateTime.Now;

        // List of criteria with their questions
        public List<CriteriaWithQuestionsViewModel> CriteriaGroups { get; set; } = new();
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

        [Range(0, 4, ErrorMessage = "Score must be between 0 and 4")]
        public int ScoreValue { get; set; }
    }

    // ViewModel for submitting the evaluation
    public class SubmitEvaluationViewModel
    {
        [Required(ErrorMessage = "Please select a teacher")]
        [Display(Name = "Teacher")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Please select a subject")]
        [Display(Name = "Subject")]
        public int SubjectId { get; set; }

        [Display(Name = "Submit anonymously")]
        public bool IsAnonymous { get; set; } = true;

        [MaxLength(1000)]
        [Display(Name = "Additional Comments or Suggestions")]
        public string? Comments { get; set; }

        [Required]
        public List<ScoreViewModel> Scores { get; set; } = new();
    }

    public class ScoreViewModel
    {
        [Required]
        public int QuestionId { get; set; }

        [Required(ErrorMessage = "Please rate this item")]
        [Range(0, 4, ErrorMessage = "Score must be between 0 and 4")]
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

        // Distribution of scores (how many 0s, 1s, 2s, 3s, 4s)
        public Dictionary<int, int> ScoreDistribution { get; set; } = new();
    }

    // ViewModel for the table display
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

    // ViewModel for criteria management
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