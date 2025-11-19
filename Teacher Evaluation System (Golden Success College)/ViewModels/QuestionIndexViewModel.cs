namespace Teacher_Evaluation_System__Golden_Success_College_.ViewModels
{
    public class QuestionIndexViewModel
    {
        public int CriteriaId { get; set; }
        public string CriteriaName { get; set; } = "";
        public List<string> Descriptions { get; set; } = new List<string>();
        public List<int> QuestionIds { get; set; } = new List<int>();
    }
}
