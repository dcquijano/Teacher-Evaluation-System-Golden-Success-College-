using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    public class DashboardController : Controller
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public DashboardController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToAction("Login", "Auth");

            string role = User.FindFirst("RoleName")?.Value ?? "";

            // =============== ADMIN & SUPER ADMIN ==================
            if (role == "Admin" || role == "Super Admin")
            {
                ViewBag.TotalTeachers = _context.Teacher.Count();
                ViewBag.TotalStudents = _context.Student.Count();
                ViewBag.TotalEvaluations = _context.Evaluation.Count();

                return View("Index");   // <-- FIXED HERE
            }

            // ====================== STUDENT =======================
            if (role == "Student")
            {
                var teachers = _context.Teacher
                    .Where(t => t.IsActive)
                    .Include(t => t.Level)
                    .ToList();

                return View("Index", teachers);  // <-- FIXED HERE
            }

            // ========== UNKNOWN ROLE → DENIED ACCESS ===============
            return RedirectToAction("AccessDenied", "Auth");
        }
    }
}
