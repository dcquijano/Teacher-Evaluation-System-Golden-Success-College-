using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;
using Teacher_Evaluation_System__Golden_Success_College_.Services;
using Teacher_Evaluation_System__Golden_Success_College_.ViewModels;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    [Authorize(Roles = "Student,Admin,Super Admin")]
    public class TeacherEvaluationsController : Controller
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;
        private readonly IActivityLogService _activityLogService;
        private readonly IEvaluationPeriodService _periodService;

        public TeacherEvaluationsController(
            Teacher_Evaluation_System__Golden_Success_College_Context context,
            IActivityLogService activityLogService,
            IEvaluationPeriodService periodService)
        {
            _context = context;
            _activityLogService = activityLogService;
            _periodService = periodService;
        }

        // GET: TeacherEvaluations/Index
        [Authorize(Roles = "Admin,Super Admin,Student")]
        public async Task<IActionResult> Index()
        {
            var studentId = GetCurrentStudentId();
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Super Admin");

            IQueryable<Evaluation> query = _context.Evaluation
                .Include(e => e.Teacher)
                .Include(e => e.Subject)
                .Include(e => e.Student)
                .Include(e => e.EvaluationPeriod)
                .Include(e => e.Scores);

            var currentPeriod = await _periodService.GetCurrentPeriodAsync();
            ViewBag.CurrentPeriod = currentPeriod;

            if (!isAdmin)
            {
                query = query.Where(e => e.StudentId == studentId);
            }

            var evaluations = await query
                .OrderByDescending(e => e.DateEvaluated)
                .Select(e => new EvaluationListItemViewModel
                {
                    EvaluationId = e.EvaluationId,
                    SubjectName = $"{e.Subject.SubjectCode} - {e.Subject.SubjectName}",
                    TeacherName = e.Teacher.FullName,
                    TeacherPicturePath = string.IsNullOrEmpty(e.Teacher.PicturePath)
                        ? "/images/default-teacher.png"
                        : e.Teacher.PicturePath,
                    StudentName = e.IsAnonymous && !isAdmin ? "Anonymous" : e.Student.FullName,
                    IsAnonymous = e.IsAnonymous,
                    DateEvaluated = e.DateEvaluated,
                    AverageScore = e.AverageScore
                })
                .ToListAsync();

            return View(evaluations);
        }

        // GET: TeacherEvaluations/Create
        [Authorize(Roles = "Admin,Super Admin,Student")]
        public async Task<IActionResult> Create()
        {
            // Check if evaluation period is active
            var currentPeriod = await _periodService.GetCurrentPeriodAsync();
            var canEvaluate = await _periodService.CanEvaluateAsync();

            if (!canEvaluate || currentPeriod == null)
            {
                if (currentPeriod == null)
                {
                    TempData["ErrorMessage"] = "No active evaluation period is currently set. Please contact the administrator.";
                }
                else if (currentPeriod.Status == "Upcoming")
                {
                    TempData["ErrorMessage"] = $"Evaluation period has not started yet. It will begin on {currentPeriod.StartDate:MMMM dd, yyyy}.";
                }
                else if (currentPeriod.Status == "Completed")
                {
                    TempData["ErrorMessage"] = $"Evaluation period has ended on {currentPeriod.EndDate:MMMM dd, yyyy}. Please contact the administrator.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Evaluation period is not active. Period: {currentPeriod.PeriodName} ({currentPeriod.StartDate:MMM dd, yyyy} - {currentPeriod.EndDate:MMM dd, yyyy})";
                }
                return RedirectToAction(nameof(Index));
            }

            // Display current period information
            ViewBag.CurrentPeriod = currentPeriod;
            TempData["InfoMessage"] = $"Evaluating for: {currentPeriod.PeriodName} ({currentPeriod.Semester}, {currentPeriod.AcademicYear})";

            var studentId = GetCurrentStudentId();
            var student = await _context.Student.FindAsync(studentId);

            var availablePairs = await GetAvailableTeacherSubjectPairs(studentId, currentPeriod.EvaluationPeriodId);

            if (!availablePairs.Any())
            {
                ViewBag.HasAvailableEnrollments = false;
                TempData["Message"] = "You have completed all evaluations for this period. Thank you!";

                var emptyViewModel = new EvaluationFormViewModel
                {
                    StudentId = studentId,
                    StudentName = student?.FullName
                };

                ViewBag.Teachers = new List<SelectListItem>();
                ViewBag.Subjects = new List<SelectListItem>();
                ViewBag.Students = new SelectList(new[] { new { Value = studentId, Text = student?.FullName } }, "Value", "Text", studentId);

                return View(emptyViewModel);
            }

            var teachers = availablePairs
                .Select(x => x.Teacher)
                .DistinctBy(t => t.TeacherId)
                .OrderBy(t => t.FullName)
                .Select(t => new SelectListItem
                {
                    Value = t.TeacherId.ToString(),
                    Text = t.FullName
                })
                .ToList();

            var criteriaGroups = await _context.Question
                .Include(q => q.Criteria)
                .OrderBy(q => q.CriteriaId)
                .ThenBy(q => q.QuestionId)
                .GroupBy(q => q.Criteria)
                .Select(g => new CriteriaWithQuestionsViewModel
                {
                    CriteriaId = g.Key.CriteriaId,
                    CriteriaName = g.Key.Name,
                    Questions = g.Select(q => new QuestionResponseViewModel
                    {
                        QuestionId = q.QuestionId,
                        Description = q.Description,
                        ScoreValue = 0
                    }).ToList()
                })
                .ToListAsync();

            var viewModel = new EvaluationFormViewModel
            {
                StudentId = studentId,
                StudentName = student?.FullName,
                CriteriaGroups = criteriaGroups,
                IsAnonymous = true
            };

            ViewBag.Teachers = teachers;
            ViewBag.Subjects = new List<SelectListItem>();
            ViewBag.Students = new SelectList(new[] { new { Value = studentId, Text = student?.FullName } }, "Value", "Text", studentId);
            ViewBag.HasAvailableEnrollments = true;

            return View(viewModel);
        }

        // GET: TeacherEvaluations/GetEnrolledSubjects
        [HttpGet]
        public async Task<JsonResult> GetEnrolledSubjects(int teacherId, int? studentId = null)
        {
            var currentStudentId = studentId ?? GetCurrentStudentId();

            // Get current period
            var currentPeriod = await _periodService.GetCurrentPeriodAsync();
            if (currentPeriod == null)
            {
                return Json(new List<object>());
            }

            var availablePairs = await GetAvailableTeacherSubjectPairs(currentStudentId, currentPeriod.EvaluationPeriodId);

            var subjects = availablePairs
                .Where(x => x.TeacherId == teacherId)
                .Select(x => new {
                    value = x.SubjectId,
                    text = $"{x.Subject.SubjectCode} - {x.Subject.SubjectName}"
                })
                .ToList();

            return Json(subjects);
        }

        // GET: TeacherEvaluations/GetEnrolledStudents
        [HttpGet]
        public async Task<JsonResult> GetEnrolledStudents(int teacherId, int subjectId)
        {
            // Get current period
            var currentPeriod = await _periodService.GetCurrentPeriodAsync();
            if (currentPeriod == null)
            {
                return Json(new List<object>());
            }

            var enrolledStudents = await _context.Enrollment
                .Include(e => e.Student)
                .Where(e => e.TeacherId == teacherId && e.SubjectId == subjectId)
                .Select(e => new { e.StudentId, e.Student.FullName })
                .Distinct()
                .ToListAsync();

            var evaluatedStudents = await _context.Evaluation
                .Where(e => e.TeacherId == teacherId
                    && e.SubjectId == subjectId
                    && e.EvaluationPeriodId == currentPeriod.EvaluationPeriodId)
                .Select(e => e.StudentId)
                .ToListAsync();

            var availableStudents = enrolledStudents
                .Where(s => !evaluatedStudents.Contains(s.StudentId))
                .Select(s => new {
                    value = s.StudentId,
                    text = s.FullName
                })
                .ToList();

            return Json(availableStudents);
        }

        // POST: TeacherEvaluations/Create
        [Authorize(Roles = "Admin,Super Admin,Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubmitEvaluationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please complete all required fields.";
                return RedirectToAction(nameof(Create));
            }

            // Get and validate current period
            var currentPeriod = await _periodService.GetCurrentPeriodAsync();
            if (currentPeriod == null || !currentPeriod.IsValidForEvaluation())
            {
                TempData["ErrorMessage"] = "Evaluation period is not active or has ended.";
                return RedirectToAction(nameof(Create));
            }

            var studentId = GetCurrentStudentId();
            var ipAddress = GetClientIpAddress();

            // Log evaluation started
            await _activityLogService.LogEvaluationStartedAsync(
                studentId,
                model.TeacherId,
                model.SubjectId,
                ipAddress
            );

            var isEnrolled = await _context.Enrollment
                .AnyAsync(e => e.StudentId == studentId
                    && e.TeacherId == model.TeacherId
                    && e.SubjectId == model.SubjectId);

            if (!isEnrolled)
            {
                TempData["ErrorMessage"] = "You are not enrolled in this teacher's class.";
                return RedirectToAction(nameof(Create));
            }

            // Check if already evaluated for THIS PERIOD
            var alreadyEvaluated = await _context.Evaluation
                .AnyAsync(e => e.StudentId == studentId
                    && e.TeacherId == model.TeacherId
                    && e.SubjectId == model.SubjectId
                    && e.EvaluationPeriodId == currentPeriod.EvaluationPeriodId);

            if (alreadyEvaluated)
            {
                TempData["ErrorMessage"] = "You have already evaluated this teacher for this subject in the current period.";
                return RedirectToAction(nameof(Create));
            }

            var evaluation = new Evaluation
            {
                StudentId = studentId,
                TeacherId = model.TeacherId,
                SubjectId = model.SubjectId,
                EvaluationPeriodId = currentPeriod.EvaluationPeriodId,
                IsAnonymous = model.IsAnonymous,
                DateEvaluated = DateTime.Now,
                Comments = model.Comments,
                Scores = model.Scores.Select(s => new Score
                {
                    QuestionId = s.QuestionId,
                    ScoreValue = s.ScoreValue
                }).ToList()
            };

            _context.Evaluation.Add(evaluation);
            await _context.SaveChangesAsync();

            // Log evaluation completed
            await _activityLogService.LogEvaluationCompletedAsync(
                studentId,
                evaluation.EvaluationId,
                model.TeacherId,
                model.SubjectId,
                ipAddress
            );

            TempData["SuccessMessage"] = $"Evaluation submitted successfully for {currentPeriod.PeriodName}! Thank you for your feedback.";
            return RedirectToAction(nameof(Index));
        }

        // GET: TeacherEvaluations/Details/5
        [Authorize(Roles = "Admin,Super Admin,Student")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Super Admin");

            var evaluation = await _context.Evaluation
                .Include(e => e.Teacher)
                .Include(e => e.Subject)
                .Include(e => e.Student)
                .Include(e => e.EvaluationPeriod)
                .Include(e => e.Scores)
                    .ThenInclude(s => s.Question)
                        .ThenInclude(q => q.Criteria)
                .FirstOrDefaultAsync(e => e.EvaluationId == id);

            if (evaluation == null)
                return NotFound();

            if (!isAdmin)
            {
                var currentStudentId = GetCurrentStudentId();

                if (currentStudentId == null)
                    return Unauthorized();

                if (evaluation.StudentId != currentStudentId)
                    return Unauthorized();
            }

            string studentNameToShow =
                evaluation.IsAnonymous && !isAdmin
                    ? "Anonymous"
                    : evaluation.Student?.FullName;

            var viewModel = new EvaluationResultViewModel
            {
                EvaluationId = evaluation.EvaluationId,
                TeacherName = evaluation.Teacher?.FullName,
                TeacherPicturePath = evaluation.Teacher?.PicturePath,
                TeacherDepartment = evaluation.Teacher?.Department,
                SubjectName = $"{evaluation.Subject?.SubjectCode} - {evaluation.Subject?.SubjectName}",
                StudentName = studentNameToShow,
                IsAnonymous = evaluation.IsAnonymous,
                DateEvaluated = evaluation.DateEvaluated,
                Comments = evaluation.Comments,
                OverallAverage = evaluation.AverageScore,
                CriteriaResults = evaluation.Scores
                    .GroupBy(s => s.Question?.Criteria?.Name)
                    .Select(g => new CriteriaResultViewModel
                    {
                        CriteriaName = g.Key,
                        CriteriaAverage = g.Average(s => s.ScoreValue),
                        Questions = g.Select(s => new QuestionResultViewModel
                        {
                            Description = s.Question?.Description,
                            ScoreValue = s.ScoreValue
                        }).ToList()
                    }).ToList()
            };

            // Add evaluation period info to ViewBag
            if (evaluation.EvaluationPeriod != null)
            {
                ViewBag.EvaluationPeriod = evaluation.EvaluationPeriod;
            }

            return View(viewModel);
        }

        // POST: TeacherEvaluations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Super Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var evaluation = await _context.Evaluation
                .Include(e => e.Scores)
                .FirstOrDefaultAsync(e => e.EvaluationId == id);

            if (evaluation != null)
            {
                // STEP 1: Delete related ActivityLogs first (to avoid FK constraint)
                var relatedLogs = await _context.ActivityLog
                    .Where(a => a.EvaluationId == id)
                    .ToListAsync();

                if (relatedLogs.Any())
                {
                    _context.ActivityLog.RemoveRange(relatedLogs);
                }

                // STEP 2: Delete Scores (cascade will handle this, but being explicit)
                _context.Score.RemoveRange(evaluation.Scores);

                // STEP 3: Delete Evaluation
                _context.Evaluation.Remove(evaluation);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Evaluation deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // DELETE: api/TeacherEvaluationsApi/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Super Admin")]
        public async Task<IActionResult> DeleteEvaluation(int id)
        {
            try
            {
                var evaluation = await _context.Evaluation
                    .Include(e => e.Scores)
                    .FirstOrDefaultAsync(e => e.EvaluationId == id);

                if (evaluation == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Evaluation not found"
                    });
                }

                // STEP 1: Delete related ActivityLogs first
                var relatedLogs = await _context.ActivityLog
                    .Where(a => a.EvaluationId == id)
                    .ToListAsync();

                if (relatedLogs.Any())
                {
                    _context.ActivityLog.RemoveRange(relatedLogs);
                }

                // STEP 2: Delete Scores
                _context.Score.RemoveRange(evaluation.Scores);

                // STEP 3: Delete Evaluation
                _context.Evaluation.Remove(evaluation);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Evaluation deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error deleting evaluation: {ex.Message}"
                });
            }
        }

        private int GetCurrentStudentId()
        {
            var studentIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentIdClaim))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }
            return int.Parse(studentIdClaim);
        }

        private string GetClientIpAddress()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
            }

            return ipAddress ?? "Unknown";
        }

        private async Task<List<Enrollment>> GetAvailableTeacherSubjectPairs(int studentId, int evaluationPeriodId)
        {
            var enrollments = await _context.Enrollment
                .Include(e => e.Teacher)
                .Include(e => e.Subject)
                .Where(e => e.StudentId == studentId && e.Teacher.IsActive)
                .ToListAsync();

            // Filter out already evaluated pairs for THIS PERIOD
            var evaluatedPairs = await _context.Evaluation
                .Where(e => e.StudentId == studentId && e.EvaluationPeriodId == evaluationPeriodId)
                .Select(e => new { e.TeacherId, e.SubjectId })
                .ToListAsync();

            var available = enrollments
                .Where(enrollment => !evaluatedPairs.Any(ep =>
                    ep.TeacherId == enrollment.TeacherId &&
                    ep.SubjectId == enrollment.SubjectId))
                .ToList();

            return available;
        }
    }
}