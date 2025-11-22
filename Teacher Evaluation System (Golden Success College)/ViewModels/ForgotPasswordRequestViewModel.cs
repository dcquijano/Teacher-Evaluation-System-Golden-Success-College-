using System.ComponentModel.DataAnnotations;

namespace Teacher_Evaluation_System__Golden_Success_College_.ViewModels
{
    public class ForgotPasswordRequestViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;
    }
}
