using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
        public async Task<IActionResult> Index(string? filterTeacher, string? filterSubject,
            DateTime? filterDateFrom, DateTime? filterDateTo)
        {
            var query = _context.Evaluation
                .Include(e => e.Subject)
                .Include(e => e.Teacher)
                .Include(e => e.Student)
                .Include(e => e.Scores)
                .AsQueryable();

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
                    StudentName = e.IsAnonymous ? "Anonymous" : e.Student?.FullName,
                    IsAnonymous = e.IsAnonymous,
                    DateEvaluated = e.DateEvaluated,
                    Comments = e.Comments,
                    AverageScore = e.AverageScore
                }).ToList()
            };

            return View(viewModel);
        }



        // GET: Evaluations/Create
        public async Task<IActionResult> Create(int? teacherId, int? subjectId, int? studentId)
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
                StudentId = studentId ?? 0,
                TeacherPicturePath = teacherPicturePath,
                TeacherDepartment = teacherDepartment,
                TeacherName = teacherName,
                CriteriaGroups = criteriaGroups,
                DateEvaluated = DateTime.Now
            };

            // Populate dropdowns
            await PopulateDropdowns(teacherId, subjectId, studentId);

            return View(viewModel);
        }

        // POST: Evaluations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubmitEvaluationViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Validate that all questions have been answered
                    var totalQuestions = await _context.Question.CountAsync();
                    if (model.QuestionScores.Count != totalQuestions)
                    {
                        ModelState.AddModelError("", "Please answer all questions.");
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
                StudentName = evaluation.IsAnonymous ? "Anonymous" : evaluation.Student?.FullName,
                IsAnonymous = evaluation.IsAnonymous,
                DateEvaluated = evaluation.DateEvaluated,
                Comments = evaluation.Comments,
                OverallAverage = evaluation.AverageScore,
                CriteriaResults = criteriaResults
            };

            return View(viewModel);
        }

        // GET: Evaluations/TeacherSummary/5
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

            return View(evaluation);
        }

        // POST: Evaluations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
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

        // Helper method to populate dropdowns
        private async Task PopulateDropdowns(int? teacherId = null, int? subjectId = null, int? studentId = null)
        {
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

            ViewBag.Students = new SelectList(
                await _context.Student.OrderBy(s => s.FullName).ToListAsync(),
                "StudentId",
                "FullName",
                studentId
            );
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
                        ScoreValue = model.QuestionScores
                            .Where(qs => qs.QuestionId == q.QuestionId)
                            .Select(qs => qs.ScoreValue)
                            .FirstOrDefault()
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