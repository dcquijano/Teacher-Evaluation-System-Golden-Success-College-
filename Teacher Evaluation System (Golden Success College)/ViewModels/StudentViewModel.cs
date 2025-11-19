using System.ComponentModel.DataAnnotations;

namespace Teacher_Evaluation_System__Golden_Success_College_.ViewModels
{
    public class StudentViewModel
    {
        public SchoolLevel Level { get; set; }  // Enum property


        public enum SchoolLevel
        {
            [Display(Name = "High School")]
            HighSchool = 1,

            [Display(Name = "Senior High")]
            SeniorHigh = 2,

            [Display(Name = "College")]
            College = 3
        }

    }
}
