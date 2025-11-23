using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;
using Teacher_Evaluation_System__Golden_Success_College_.Services;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student,Admin,Super Admin")]
    public class TeacherEvaluationsApiController : ControllerBase
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;
        private readonly IActivityLogService _activityLogService;
        private readonly IEvaluationPeriodService _periodService;

        public TeacherEvaluationsApiController(
            Teacher_Evaluation_System__Golden_Success_College_Context context,
            IActivityLogService activityLogService,
            IEvaluationPeriodService periodService)
        {
            _context = context;
            _activityLogService = activityLogService;
            _periodService = periodService;
        }

        // GET: api/TeacherEvaluationsApi
        [HttpGet]
        public async Task<IActionResult> GetEvaluations(
            [FromQuery] string? filterTeacher,
            [FromQuery] string? filterSubject,
            [FromQuery] DateTime? filterDateFrom,
            [FromQuery] DateTime? filterDateTo,
            [FromQuery] int? filterPeriodId)
        {
            try
            {
                var studentId = GetCurrentStudentId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("Super Admin");

                IQueryable<Evaluation> query = _context.Evaluation
                    .Include(e => e.Teacher)
                    .Include(e => e.Subject)
                    .Include(e => e.Student)
                    .Include(e => e.EvaluationPeriod)
                    .Include(e => e.Scores)
                        .ThenInclude(s => s.Question)
                            .ThenInclude(q => q.Criteria);

                // If student – restrict to their own evaluations
                if (!isAdmin)
                {
                    query = query.Where(e => e.StudentId == studentId);
                }

                // Apply filters
                if (!string.IsNullOrWhiteSpace(filterTeacher))
                    query = query.Where(e => e.Teacher.FullName.Contains(filterTeacher));

                if (!string.IsNullOrWhiteSpace(filterSubject))
                    query = query.Where(e =>
                        e.Subject.SubjectName.Contains(filterSubject) ||
                        e.Subject.SubjectCode.Contains(filterSubject));

                if (filterDateFrom.HasValue)
                    query = query.Where(e => e.DateEvaluated >= filterDateFrom.Value.Date);

                if (filterDateTo.HasValue)
                    query = query.Where(e => e.DateEvaluated <= filterDateTo.Value.Date.AddDays(1).AddTicks(-1));

                if (filterPeriodId.HasValue)
                    query = query.Where(e => e.EvaluationPeriodId == filterPeriodId.Value);

                var evaluations = await query
                    .OrderByDescending(e => e.DateEvaluated)
                    .Select(e => new
                    {
                        e.EvaluationId,
                        SubjectName = $"{e.Subject.SubjectCode} - {e.Subject.SubjectName}",
                        TeacherName = e.Teacher.FullName,
                        TeacherPicturePath = string.IsNullOrEmpty(e.Teacher.PicturePath)
                            ? "/images/default-teacher.png"
                            : e.Teacher.PicturePath,
                        StudentName = e.IsAnonymous && !isAdmin ? "Anonymous" : e.Student.FullName,
                        e.IsAnonymous,
                        e.DateEvaluated,
                        e.Comments,
                        e.AverageScore,
                        Period = e.EvaluationPeriod != null ? new
                        {
                            e.EvaluationPeriod.EvaluationPeriodId,
                            e.EvaluationPeriod.PeriodName,
                            e.EvaluationPeriod.AcademicYear,
                            e.EvaluationPeriod.Semester
                        } : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = evaluations,
                    message = "Evaluations retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving evaluations: {ex.Message}"
                });
            }
        }

        // GET: api/TeacherEvaluationsApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvaluation(int id)
        {
            try
            {
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
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Evaluation not found"
                    });
                }

                // Security check
                if (!isAdmin)
                {
                    var currentStudentId = GetCurrentStudentId();
                    if (evaluation.StudentId != currentStudentId)
                    {
                        return Unauthorized(new
                        {
                            success = false,
                            message = "Unauthorized access"
                        });
                    }
                }

                var result = new
                {
                    evaluation.EvaluationId,
                    TeacherName = evaluation.Teacher?.FullName,
                    TeacherPicturePath = evaluation.Teacher?.PicturePath,
                    TeacherDepartment = evaluation.Teacher?.Department,
                    SubjectName = $"{evaluation.Subject?.SubjectCode} - {evaluation.Subject?.SubjectName}",
                    StudentName = evaluation.IsAnonymous && !isAdmin ? "Anonymous" : evaluation.Student?.FullName,
                    evaluation.IsAnonymous,
                    evaluation.DateEvaluated,
                    evaluation.Comments,
                    OverallAverage = evaluation.AverageScore,
                    Period = evaluation.EvaluationPeriod != null ? new
                    {
                        evaluation.EvaluationPeriod.EvaluationPeriodId,
                        evaluation.EvaluationPeriod.PeriodName,
                        evaluation.EvaluationPeriod.AcademicYear,
                        evaluation.EvaluationPeriod.Semester,
                        evaluation.EvaluationPeriod.StartDate,
                        evaluation.EvaluationPeriod.EndDate
                    } : null,
                    CriteriaResults = evaluation.Scores
                        .GroupBy(s => new { s.Question.Criteria.CriteriaId, s.Question.Criteria.Name })
                        .Select(g => new
                        {
                            CriteriaName = g.Key.Name,
                            CriteriaAverage = g.Average(s => s.ScoreValue),
                            Questions = g.Select(s => new
                            {
                                Description = s.Question.Description,
                                s.ScoreValue
                            }).ToList()
                        }).ToList()
                };

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Evaluation retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving evaluation: {ex.Message}"
                });
            }
        }

        // POST: api/TeacherEvaluationsApi
        [HttpPost]
        public async Task<IActionResult> CreateEvaluation([FromBody] CreateEvaluationDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid data",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                // Get and validate current period
                var currentPeriod = await _periodService.GetCurrentPeriodAsync();
                if (currentPeriod == null || !currentPeriod.IsValidForEvaluation())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Evaluation period is not active or has ended"
                    });
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

                // Verify enrollment
                var isEnrolled = await _context.Enrollment
                    .AnyAsync(e => e.StudentId == studentId
                        && e.TeacherId == model.TeacherId
                        && e.SubjectId == model.SubjectId);

                if (!isEnrolled)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "You are not enrolled in this teacher's class"
                    });
                }

                // Check if already evaluated FOR THIS PERIOD
                var alreadyEvaluated = await _context.Evaluation
                    .AnyAsync(e => e.StudentId == studentId
                        && e.TeacherId == model.TeacherId
                        && e.SubjectId == model.SubjectId
                        && e.EvaluationPeriodId == currentPeriod.EvaluationPeriodId);

                if (alreadyEvaluated)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "You have already evaluated this teacher for this subject in the current period"
                    });
                }

                // Create evaluation
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

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        evaluation.EvaluationId,
                        Period = new
                        {
                            currentPeriod.PeriodName,
                            currentPeriod.AcademicYear,
                            currentPeriod.Semester
                        }
                    },
                    message = "Evaluation submitted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error creating evaluation: {ex.Message}"
                });
            }
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

                // STEP 2: Remove scores
                _context.Score.RemoveRange(evaluation.Scores);

                // STEP 3: Remove evaluation
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

        // Helper methods
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
    }

    // DTOs for API
    public class CreateEvaluationDto
    {
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
        public bool IsAnonymous { get; set; }
        public string? Comments { get; set; }
        public List<ScoreDto> Scores { get; set; } = new();
    }

    public class ScoreDto
    {
        public int QuestionId { get; set; }
        public int ScoreValue { get; set; }
    }
}