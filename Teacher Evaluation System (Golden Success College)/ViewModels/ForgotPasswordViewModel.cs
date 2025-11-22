using System.ComponentModel.DataAnnotations;

namespace Teacher_Evaluation_System__Golden_Success_College_.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty; // Token from email link

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
