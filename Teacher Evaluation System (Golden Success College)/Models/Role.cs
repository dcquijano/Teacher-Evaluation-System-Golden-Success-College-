using System.ComponentModel.DataAnnotations;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        public string? Name { get; set; }  // e.g., "Super Admin", "Admin", "Student"

        public ICollection<User>? Users { get; set; }
    }
}
