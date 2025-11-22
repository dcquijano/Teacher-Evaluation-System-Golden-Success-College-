using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.ViewModels;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    [Authorize(Roles = "Admin,Super Admin,Student")]
    public class DashboardController : Controller
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public DashboardController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

    
        public async Task<IActionResult> Index()
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToAction("Login", "Auth");

            string role = User.FindFirst("RoleName")?.Value ?? "";

            // Use built-in role checking
            if (User.IsInRole("Admin") || User.IsInRole("Super Admin"))
            {
                ViewBag.TotalTeachers = await _context.Teacher.CountAsync();
                ViewBag.TotalStudents = await _context.Student.CountAsync();
                ViewBag.TotalEvaluations = await _context.Evaluation.CountAsync();
                return View();
            }

            // ====================== STUDENT =======================
            if (User.IsInRole("Student"))
            {
                string userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out int studentId))
                    return RedirectToAction("AccessDenied", "Auth");

                // Get enrolled teacher IDs
                var enrolledTeacherIds = await _context.Enrollment
                    .Where(e => e.StudentId == studentId)
                    .Select(e => e.TeacherId)
                    .ToListAsync();

                // Get evaluated teacher IDs
                var evaluatedTeacherIds = await _context.Evaluation
                    .Where(e => e.StudentId == studentId)
                    .Select(e => e.TeacherId)
                    .ToListAsync();

                // 1. Get teachers to evaluate
                var teachersToEvaluate = await _context.Teacher
                    .Where(t => enrolledTeacherIds.Contains(t.TeacherId) && !evaluatedTeacherIds.Contains(t.TeacherId))
                    .Include(t => t.Level)
                    .ToListAsync();

                // 2. Get enrollments for student, filter by those teachers
                var enrollments = await _context.Enrollment
                    .Where(e => e.StudentId == studentId && teachersToEvaluate.Select(t => t.TeacherId).Contains(e.TeacherId))
                    .Include(e => e.Subject)
                    .ToListAsync();

                // 3. Map each teacher to first available subject (client-side)
                var teachersWithSubjects = teachersToEvaluate.Select(t =>
                {
                    var firstSubject = enrollments
                        .Where(e => e.TeacherId == t.TeacherId)
                        .Select(e => e.Subject)
                        .FirstOrDefault();

                    return new TeacherWithSubjectViewModel
                    {
                        Teacher = t,
                        FirstSubjectId = firstSubject?.SubjectId ?? 0
                    };
                }).ToList();

                // Pass strongly-typed model to view
                return View(teachersWithSubjects);
            }

            return RedirectToAction("AccessDenied", "Auth");
        }
    }
}
