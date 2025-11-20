using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;
using Teacher_Evaluation_System__Golden_Success_College_.ViewModels;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    public class TeacherEvaluationsController : Controller
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public TeacherEvaluationsController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: Evaluations
        [Authorize(Roles = "Student,Admin,Super Admin")]
        public async Task<IActionResult> Index(string? filterTeacher, string? filterSubject,
            DateTime? filterDateFrom, DateTime? filterDateTo)
        {
            var query = _context.Evaluation
                .Include(e => e.Subject)
                .Include(e => e.Teacher)
                .Include(e => e.Student)
                .Include(e => e.Scores)
                .AsQueryable();

            // Check if user is Admin or SuperAdmin
            var isAdminOrSuperAdmin = User.IsInRole("Super Admin") || User.IsInRole("Admin");

            // If user is a student, only show their evaluations
            if (User.IsInRole("Student") && !isAdminOrSuperAdmin)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                query = query.Where(e => e.StudentId == userId);
            }

            // Apply filters
            if (!string.IsNullOrEmpty(filterTeacher))
            {
                query = query.Where(e => e.Teacher!.FullName!.Contains(filterTeacher));
            }

            if (!string.IsNullOrEmpty(filterSubject))
            {
                query = query.Where(e => e.Subject!.SubjectName!.Contains(filterSubject));
            }

            if (filterDateFrom.HasValue)
            {
                query = query.Where(e => e.DateEvaluated >= filterDateFrom.Value);
            }

            if (filterDateTo.HasValue)
            {
                query = query.Where(e => e.DateEvaluated <= filterDateTo.Value);
            }

            var evaluations = await query
                .OrderByDescending(e => e.DateEvaluated)
                .ToListAsync();

            var viewModel = new EvaluationListViewModel
            {
                FilterTeacherName = filterTeacher,
                FilterSubjectName = filterSubject,
                FilterDateFrom = filterDateFrom,
                FilterDateTo = filterDateTo,
                Evaluations = evaluations.Select(e => new EvaluationListItemViewModel
                {
                    EvaluationId = e.EvaluationId,
                    SubjectName = e.Subject?.SubjectName,
                    TeacherName = e.Teacher?.FullName,
                    TeacherPicturePath = e.Teacher?.PicturePath,
                    // Only show student name if: NOT anonymous OR user is Admin/SuperAdmin
                    StudentName = (e.IsAnonymous && !isAdminOrSuperAdmin) ? "Anonymous" : e.Student?.FullName,
                    IsAnonymous = e.IsAnonymous,
                    DateEvaluated = e.DateEvaluated,
                    Comments = e.Comments,
                    AverageScore = e.AverageScore
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Evaluations/Create - Only Students can create
        [Authorize(Roles = "Student,Admin,Super Admin")]
        public async Task<IActionResult> Create(int? teacherId, int? subjectId, int? studentId)
        {
            // Get logged-in user ID
            var loggedInUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userType = User.FindFirstValue("UserType");
            var isAdminOrSuperAdmin = User.IsInRole("Admin") || User.IsInRole("Super Admin");

            // For students, use their own ID
            // For admins, allow them to specify or leave blank
            int actualStudentId = studentId ?? 0;
            if (userType == "Student" && !isAdminOrSuperAdmin)
            {
                actualStudentId = loggedInUserId;
            }

            // Load all criteria
            var allCriteria = await _context.Criteria
                .OrderBy(c => c.CriteriaId)
                .ToListAsync();

            // Load all questions
            var allQuestions = await _context.Question
                .OrderBy(q => q.CriteriaId)
                .ThenBy(q => q.QuestionId)
                .ToListAsync();

            // Group questions by criteria
            var criteriaGroups = allCriteria.Select(c => new CriteriaWithQuestionsViewModel
            {
                CriteriaId = c.CriteriaId,
                CriteriaName = c.Name,
                Questions = allQuestions
                    .Where(q => q.CriteriaId == c.CriteriaId)
                    .Select(q => new QuestionResponseViewModel
                    {
                        QuestionId = q.QuestionId,
                        Description = q.Description,
                        ScoreValue = 0
                    }).ToList()
            }).ToList();

            // Load teacher info if teacherId is provided
            string? teacherPicturePath = null;
            string? teacherDepartment = null;
            string? teacherName = null;
            if (teacherId.HasValue && teacherId.Value > 0)
            {
                var teacher = await _context.Teacher.FindAsync(teacherId.Value);
                if (teacher != null)
                {
                    teacherPicturePath = teacher.PicturePath;
                    teacherDepartment = teacher.Department;
                    teacherName = teacher.FullName;
                }
            }

            var viewModel = new EvaluationFormViewModel
            {
                TeacherId = teacherId ?? 0,
                SubjectId = subjectId ?? 0,
                StudentId = actualStudentId,
                TeacherPicturePath = teacherPicturePath,
                TeacherDepartment = teacherDepartment,
                TeacherName = teacherName,
                CriteriaGroups = criteriaGroups,
                DateEvaluated = DateTime.Now
            };

            // Populate dropdowns
            await PopulateDropdowns(teacherId, subjectId, actualStudentId);

            return View(viewModel);
        }

        // POST: Evaluations/Create - Only Students can submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student,Admin,Super Admin")]
        public async Task<IActionResult> Create(SubmitEvaluationViewModel model)
        {
            // Get logged-in user info
            var loggedInUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userType = User.FindFirstValue("UserType");
            var isAdminOrSuperAdmin = User.IsInRole("Admin") || User.IsInRole("Super Admin");

            // For students, override the StudentId with their own ID
            if (userType == "Student" && !isAdminOrSuperAdmin)
            {
                model.StudentId = loggedInUserId;
            }

            // Validate StudentId
            if (model.StudentId <= 0)
            {
                ModelState.AddModelError("StudentId", "Please select a student.");
            }

            // Validate TeacherId
            if (model.TeacherId <= 0)
            {
                ModelState.AddModelError("TeacherId", "Please select a teacher.");
            }

            // Validate SubjectId
            if (model.SubjectId <= 0)
            {
                ModelState.AddModelError("SubjectId", "Please select a subject.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Validate that all questions have been answered
                    var totalQuestions = await _context.Question.CountAsync();
                    if (model.QuestionScores == null || model.QuestionScores.Count != totalQuestions)
                    {
                        ModelState.AddModelError("", "Please answer all questions.");
                        return await RedirectToCreateWithError(model);
                    }

                    // Check for duplicate evaluation (same student, teacher, subject on same day)
                    var today = DateTime.Today;
                    var tomorrow = today.AddDays(1);
                    var existingEvaluation = await _context.Evaluation
                        .AnyAsync(e => e.StudentId == model.StudentId
                                    && e.TeacherId == model.TeacherId
                                    && e.SubjectId == model.SubjectId
                                    && e.DateEvaluated >= today
                                    && e.DateEvaluated < tomorrow);

                    if (existingEvaluation)
                    {
                        ModelState.AddModelError("", "You have already evaluated this teacher for this subject today.");
                        return await RedirectToCreateWithError(model);
                    }

                    // Create the evaluation
                    var evaluation = new Evaluation
                    {
                        TeacherId = model.TeacherId,
                        SubjectId = model.SubjectId,
                        StudentId = model.StudentId,
                        IsAnonymous = model.IsAnonymous,
                        Comments = model.Comments,
                        DateEvaluated = DateTime.Now
                    };

                    _context.Evaluation.Add(evaluation);
                    await _context.SaveChangesAsync();

                    // Create scores for each question
                    var scores = model.QuestionScores.Select(qs => new Score
                    {
                        EvaluationId = evaluation.EvaluationId,
                        QuestionId = qs.QuestionId,
                        ScoreValue = qs.ScoreValue
                    }).ToList();

                    _context.Score.AddRange(scores);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Evaluation submitted successfully!";
                    return RedirectToAction(nameof(Details), new { id = evaluation.EvaluationId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error saving evaluation: {ex.Message}");
                }
            }

            return await RedirectToCreateWithError(model);
        }

        // GET: Evaluations/Details/5
        [Authorize(Roles = "Student,Admin,Super Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evaluation = await _context.Evaluation
                .Include(e => e.Subject)
                .Include(e => e.Teacher)
                .Include(e => e.Student)
                .Include(e => e.Scores)
                    .ThenInclude(s => s.Question)
                        .ThenInclude(q => q!.Criteria)
                .FirstOrDefaultAsync(m => m.EvaluationId == id);

            if (evaluation == null)
            {
                return NotFound();
            }

            // Check if user is Admin or SuperAdmin
            var isAdminOrSuperAdmin = User.IsInRole("Super Admin") || User.IsInRole("Admin");

            // If user is a student, only allow them to view their own evaluations
            if (User.IsInRole("Student") && !isAdminOrSuperAdmin)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                if (evaluation.StudentId != userId)
                {
                    return Forbid(); // Returns 403 Forbidden
                }
            }

            // Group scores by criteria
            var criteriaResults = evaluation.Scores!
                .GroupBy(s => s.Question!.Criteria)
                .Select(g => new CriteriaResultViewModel
                {
                    CriteriaName = g.Key!.Name,
                    Questions = g.Select(s => new QuestionResultViewModel
                    {
                        Description = s.Question!.Description,
                        ScoreValue = s.ScoreValue
                    }).ToList(),
                    CriteriaAverage = g.Average(s => s.ScoreValue)
                })
                .ToList();

            var viewModel = new EvaluationResultViewModel
            {
                EvaluationId = evaluation.EvaluationId,
                TeacherName = evaluation.Teacher?.FullName,
                TeacherPicturePath = evaluation.Teacher?.PicturePath,
                TeacherDepartment = evaluation.Teacher?.Department,
                SubjectName = evaluation.Subject?.SubjectName,
                // Only show student name if: NOT anonymous OR user is Admin/SuperAdmin
                StudentName = (evaluation.IsAnonymous && !isAdminOrSuperAdmin) ? "Anonymous" : evaluation.Student?.FullName,
                IsAnonymous = evaluation.IsAnonymous,
                DateEvaluated = evaluation.DateEvaluated,
                Comments = evaluation.Comments,
                OverallAverage = evaluation.AverageScore,
                CriteriaResults = criteriaResults
            };

            return View(viewModel);
        }

        // GET: Evaluations/TeacherSummary/5
        [Authorize(Roles = "Student,Admin,Super Admin")]
        public async Task<IActionResult> TeacherSummary(int? id, int? subjectId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teacher.FindAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }

            // Get all evaluations for this teacher
            var query = _context.Evaluation
                .Include(e => e.Subject)
                .Include(e => e.Scores)
                    .ThenInclude(s => s.Question)
                        .ThenInclude(q => q!.Criteria)
                .Where(e => e.TeacherId == id);

            // Filter by subject if specified
            if (subjectId.HasValue)
            {
                query = query.Where(e => e.SubjectId == subjectId);
            }

            var evaluations = await query.ToListAsync();

            if (!evaluations.Any())
            {
                ViewBag.Message = "No evaluations found for this teacher.";
                return View(new TeacherEvaluationSummaryViewModel
                {
                    TeacherId = id.Value,
                    TeacherName = teacher.FullName
                });
            }

            // Calculate summary statistics
            var allScores = evaluations.SelectMany(e => e.Scores!).ToList();

            var criteriaSummaries = allScores
                .GroupBy(s => s.Question!.Criteria)
                .Select(cg => new CriteriaSummaryViewModel
                {
                    CriteriaName = cg.Key!.Name,
                    CriteriaAverage = cg.Average(s => s.ScoreValue),
                    QuestionSummaries = cg
                        .GroupBy(s => s.Question)
                        .Select(qg => new QuestionSummaryViewModel
                        {
                            Description = qg.Key!.Description,
                            AverageScore = qg.Average(s => s.ScoreValue),
                            ResponseCount = qg.Count(),
                            ScoreDistribution = qg
                                .GroupBy(s => s.ScoreValue)
                                .ToDictionary(g => g.Key, g => g.Count())
                        })
                        .ToList()
                })
                .ToList();

            var subjectName = "All Subjects";
            if (subjectId.HasValue)
            {
                var firstEval = evaluations.FirstOrDefault();
                if (firstEval?.Subject != null)
                {
                    subjectName = firstEval.Subject.SubjectName ?? "Unknown Subject";
                }
            }

            var viewModel = new TeacherEvaluationSummaryViewModel
            {
                TeacherId = id.Value,
                TeacherName = teacher.FullName,
                SubjectName = subjectName,
                TotalEvaluations = evaluations.Count,
                OverallAverage = evaluations.Average(e => e.AverageScore),
                CriteriaSummaries = criteriaSummaries,
                RecentComments = evaluations
                    .Where(e => !string.IsNullOrEmpty(e.Comments))
                    .OrderByDescending(e => e.DateEvaluated)
                    .Take(10)
                    .Select(e => e.Comments!)
                    .ToList()
            };

            // Populate subject dropdown for filtering
            ViewBag.Subjects = new SelectList(
                await _context.Subject
                    .Where(s => _context.Evaluation
                        .Any(e => e.TeacherId == id && e.SubjectId == s.SubjectId))
                    .ToListAsync(),
                "SubjectId",
                "SubjectName",
                subjectId
            );

            return View(viewModel);
        }

        // GET: Evaluations/Delete/5
        [Authorize(Roles = "Admin,Super Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evaluation = await _context.Evaluation
                .Include(e => e.Subject)
                .Include(e => e.Teacher)
                .Include(e => e.Student)
                .Include(e => e.Scores)
                .FirstOrDefaultAsync(m => m.EvaluationId == id);

            if (evaluation == null)
            {
                return NotFound();
            }

            // Check if user is Admin or SuperAdmin for anonymous display
            var isAdminOrSuperAdmin = User.IsInRole("Super Admin") || User.IsInRole("Admin");

            // Set student name based on role and anonymity
            if (evaluation.IsAnonymous && !isAdminOrSuperAdmin)
            {
                ViewBag.StudentDisplayName = "Anonymous";
            }
            else
            {
                ViewBag.StudentDisplayName = evaluation.Student?.FullName;
            }

            return View(evaluation);
        }

        // POST: Evaluations/Delete/5
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
                // Delete all associated scores first
                _context.Score.RemoveRange(evaluation.Scores!);
                _context.Evaluation.Remove(evaluation);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Evaluation deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // AJAX endpoint to get subjects for a teacher
        [HttpGet]
        public async Task<JsonResult> GetTeacherSubjects(int teacherId)
        {
            // Get distinct subjects this teacher has been evaluated on
            var subjects = await _context.Evaluation
                .Where(e => e.TeacherId == teacherId)
                .Include(e => e.Subject)
                .Select(e => e.Subject)
                .Distinct()
                .OrderBy(s => s!.SubjectName)
                .Select(s => new {
                    value = s!.SubjectId,
                    text = s.SubjectName
                })
                .ToListAsync();

            // If no evaluations exist yet, return all subjects
            if (!subjects.Any())
            {
                subjects = await _context.Subject
                    .OrderBy(s => s.SubjectName)
                    .Select(s => new {
                        value = s.SubjectId,
                        text = s.SubjectName
                    })
                    .ToListAsync();
            }

            return Json(subjects);
        }

        private async Task PopulateDropdowns(int? teacherId = null, int? subjectId = null, int? studentId = null)
        {
            var isAdminOrSuperAdmin = User.IsInRole("Admin") || User.IsInRole("Super Admin");

            ViewBag.Teachers = new SelectList(
                await _context.Teacher
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.FullName)
                    .ToListAsync(),
                "TeacherId",
                "FullName",
                teacherId
            );

            ViewBag.Subjects = new SelectList(
                await _context.Subject.OrderBy(s => s.SubjectName).ToListAsync(),
                "SubjectId",
                "SubjectName",
                subjectId
            );

            if (isAdminOrSuperAdmin)
            {
                ViewBag.Students = new SelectList(
                    await _context.Student.OrderBy(s => s.FullName).ToListAsync(),
                    "StudentId",
                    "FullName",
                    studentId
                );
            }
            else
            {
                // For Students: only include their own record
                var loggedInUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var student = await _context.Student.FindAsync(loggedInUserId);

                ViewBag.Students = student != null
                    ? new SelectList(new List<Student> { student }, "StudentId", "FullName", studentId)
                    : new SelectList(Enumerable.Empty<Student>(), "StudentId", "FullName");
            }
        }


        // Helper method to redirect back to Create with errors
        private async Task<IActionResult> RedirectToCreateWithError(SubmitEvaluationViewModel model)
        {
            // Load all criteria
            var allCriteria = await _context.Criteria
                .OrderBy(c => c.CriteriaId)
                .ToListAsync();

            // Load all questions
            var allQuestions = await _context.Question
                .OrderBy(q => q.CriteriaId)
                .ThenBy(q => q.QuestionId)
                .ToListAsync();

            // Group questions by criteria and restore submitted scores
            var criteriaGroups = allCriteria.Select(c => new CriteriaWithQuestionsViewModel
            {
                CriteriaId = c.CriteriaId,
                CriteriaName = c.Name,
                Questions = allQuestions
                    .Where(q => q.CriteriaId == c.CriteriaId)
                    .Select(q => new QuestionResponseViewModel
                    {
                        QuestionId = q.QuestionId,
                        Description = q.Description,
                        ScoreValue = model.QuestionScores?
                            .Where(qs => qs.QuestionId == q.QuestionId)
                            .Select(qs => qs.ScoreValue)
                            .FirstOrDefault() ?? 0
                    }).ToList()
            }).ToList();

            // Load teacher info
            string? teacherPicturePath = null;
            string? teacherDepartment = null;
            string? teacherName = null;
            if (model.TeacherId > 0)
            {
                var teacher = await _context.Teacher.FindAsync(model.TeacherId);
                if (teacher != null)
                {
                    teacherPicturePath = teacher.PicturePath;
                    teacherDepartment = teacher.Department;
                    teacherName = teacher.FullName;
                }
            }

            var viewModel = new EvaluationFormViewModel
            {
                TeacherId = model.TeacherId,
                SubjectId = model.SubjectId,
                StudentId = model.StudentId,
                IsAnonymous = model.IsAnonymous,
                Comments = model.Comments,
                TeacherPicturePath = teacherPicturePath,
                TeacherDepartment = teacherDepartment,
                TeacherName = teacherName,
                CriteriaGroups = criteriaGroups
            };

            await PopulateDropdowns(model.TeacherId, model.SubjectId, model.StudentId);
            return View("Create", viewModel);
        }
    }
}