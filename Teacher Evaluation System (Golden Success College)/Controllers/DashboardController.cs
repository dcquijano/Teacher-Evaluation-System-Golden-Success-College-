using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;
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

            // Get current evaluation period for all users
            var currentPeriod = await _context.EvaluationPeriod
                .Where(p => p.IsActive && p.IsCurrent)
                .FirstOrDefaultAsync();

            ViewBag.CurrentPeriod = currentPeriod;

            // ====================== ADMIN & SUPER ADMIN =======================
            if (User.IsInRole("Admin") || User.IsInRole("Super Admin"))
            {
                ViewBag.TotalTeachers = await _context.Teacher.CountAsync();
                ViewBag.TotalStudents = await _context.Student.CountAsync();
                ViewBag.TotalEvaluations = await _context.Evaluation.CountAsync();

                // Additional statistics for current period
                if (currentPeriod != null)
                {
                    ViewBag.CurrentPeriodEvaluations = await _context.Evaluation
                        .Where(e => e.EvaluationPeriodId == currentPeriod.EvaluationPeriodId)
                        .CountAsync();

                    // Calculate average score correctly - get all scores from evaluations in this period
                    var periodScores = await _context.Score
                        .Where(s => s.Evaluation.EvaluationPeriodId == currentPeriod.EvaluationPeriodId)
                        .ToListAsync();

                    ViewBag.CurrentPeriodAverage = periodScores.Any()
                        ? periodScores.Average(s => (double)s.ScoreValue)
                        : 0;

                    // Count unique students who evaluated in current period
                    ViewBag.CurrentPeriodParticipants = await _context.Evaluation
                        .Where(e => e.EvaluationPeriodId == currentPeriod.EvaluationPeriodId)
                        .Select(e => e.StudentId)
                        .Distinct()
                        .CountAsync();

                    // Total possible evaluations (enrolled students × teachers)
                    var totalEnrollments = await _context.Enrollment
                        .Select(e => new { e.StudentId, e.TeacherId })
                        .Distinct()
                        .CountAsync();

                    ViewBag.TotalPossibleEvaluations = totalEnrollments;

                    // Completion percentage
                    if (totalEnrollments > 0)
                    {
                        ViewBag.CompletionPercentage = Math.Round(
                            (double)ViewBag.CurrentPeriodEvaluations / totalEnrollments * 100, 2);
                    }
                    else
                    {
                        ViewBag.CompletionPercentage = 0;
                    }
                }

                return View(null); // Admin dashboard does not use a model
            }

            // ====================== STUDENT =======================
            if (User.IsInRole("Student"))
            {
                string userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out int studentId))
                    return RedirectToAction("AccessDenied", "Auth");

                // Check if there's an active evaluation period
                if (currentPeriod == null)
                {
                    TempData["ErrorMessage"] = "No active evaluation period is currently available. Please contact the administrator.";
                    return View(new List<TeacherWithSubjectViewModel>());
                }

                // Check if period is valid for evaluation
                if (!currentPeriod.IsValidForEvaluation())
                {
                    if (currentPeriod.Status == "Upcoming")
                    {
                        TempData["InfoMessage"] = $"Evaluation period '{currentPeriod.PeriodName}' will start on {currentPeriod.StartDate:MMMM dd, yyyy}.";
                    }
                    else if (currentPeriod.Status == "Completed")
                    {
                        TempData["ErrorMessage"] = $"Evaluation period '{currentPeriod.PeriodName}' has ended on {currentPeriod.EndDate:MMMM dd, yyyy}.";
                    }
                    return View(new List<TeacherWithSubjectViewModel>());
                }

                // Get enrolled teacher IDs
                var enrolledTeacherIds = await _context.Enrollment
                    .Where(e => e.StudentId == studentId)
                    .Select(e => e.TeacherId)
                    .Distinct()
                    .ToListAsync();

                // Get already evaluated teachers for this period
                var evaluatedTeacherIds = await _context.Evaluation
                    .Where(e => e.StudentId == studentId &&
                                e.EvaluationPeriodId == currentPeriod.EvaluationPeriodId)
                    .Select(e => e.TeacherId)
                    .Distinct()
                    .ToListAsync();

                // Filter teachers TO evaluate
                var teachersToEvaluate = await _context.Teacher
                    .Where(t => enrolledTeacherIds.Contains(t.TeacherId) &&
                                !evaluatedTeacherIds.Contains(t.TeacherId) &&
                                t.IsActive)
                    .Include(t => t.Level)
                    .OrderBy(t => t.FullName)
                    .ToListAsync();

                // Get subjects for filtered teachers
                var enrollments = await _context.Enrollment
                    .Where(e => e.StudentId == studentId &&
                                teachersToEvaluate.Select(t => t.TeacherId)
                                    .Contains(e.TeacherId))
                    .Include(e => e.Subject)
                    .ToListAsync();

                // Map ViewModel
                var teachersWithSubjects = teachersToEvaluate.Select(t =>
                {
                    var subject = enrollments
                        .Where(e => e.TeacherId == t.TeacherId)
                        .Select(e => e.Subject)
                        .FirstOrDefault();

                    return new TeacherWithSubjectViewModel
                    {
                        Teacher = t,
                        FirstSubjectId = subject?.SubjectId ?? 0
                    };
                }).ToList();

                // Calculate student progress
                var totalEnrolled = enrolledTeacherIds.Count;
                var totalEvaluated = evaluatedTeacherIds.Count;
                var totalRemaining = teachersWithSubjects.Count;

                ViewBag.TotalEnrolled = totalEnrolled;
                ViewBag.TotalEvaluated = totalEvaluated;
                ViewBag.TotalRemaining = totalRemaining;

                if (totalEnrolled > 0)
                {
                    ViewBag.ProgressPercentage = Math.Round((double)totalEvaluated / totalEnrolled * 100, 2);
                }
                else
                {
                    ViewBag.ProgressPercentage = 0;
                }

                // Show completion message if all evaluations done
                if (totalRemaining == 0 && totalEnrolled > 0)
                {
                    TempData["SuccessMessage"] = $"🎉 Congratulations! You have completed all {totalEvaluated} evaluations for {currentPeriod.PeriodName}. Thank you for your feedback!";
                }
                else if (totalRemaining > 0)
                {
                    TempData["InfoMessage"] = $"You have {totalRemaining} teacher(s) remaining to evaluate for {currentPeriod.PeriodName}.";
                }

                return View(teachersWithSubjects);
            }

            return RedirectToAction("AccessDenied", "Auth");
        }
    }
}