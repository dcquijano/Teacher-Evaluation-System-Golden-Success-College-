using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Helper;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    [Authorize(Roles = "Student")]
    public class AccountController : Controller
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public AccountController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // ==============================
        // PROFILE INFORMATION
        // ==============================
        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var student = await _context.Student
                .Include(x => x.Level)
                .Include(x => x.Section)
                .FirstOrDefaultAsync(x => x.StudentId == userId);

            if (student == null)
                return NotFound();

            return View(student);
        }

        // ==============================
        // CHANGE PASSWORD
        // ==============================
        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string currentPassword, string newPassword)
        {
            var student = await _context.Student.FindAsync(id);
            if (student == null)
                return NotFound();

            // Validate current password
            if (!PasswordHelper.VerifyPassword(currentPassword, student.Password))
                return Json(new { success = false, message = "Current password is incorrect" });

            // Update new password
            student.Password = PasswordHelper.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Password updated successfully" });
        }
    }
}
