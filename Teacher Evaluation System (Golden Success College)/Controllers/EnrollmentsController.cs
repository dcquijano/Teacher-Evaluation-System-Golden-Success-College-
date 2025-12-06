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
    [Authorize(Roles = "Admin,Super Admin,Student")]
    public class EnrollmentsController : Controller
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public EnrollmentsController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: Enrollments
        public async Task<IActionResult> Index(int? levelId, int? sectionId)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            ViewBag.UserRole = userRole;

            // Populate filter dropdowns for Admin/Super Admin
            ViewBag.Levels = new SelectList(await _context.Level.OrderBy(l => l.LevelName).ToListAsync(), "LevelId", "LevelName", levelId);
            ViewBag.SelectedLevelId = levelId;
            ViewBag.SelectedSectionId = sectionId;

            // Build query based on role
            var query = _context.Enrollment
                .Include(e => e.Student)
                    .ThenInclude(s => s.Section)
                        .ThenInclude(sec => sec.Level)
                .Include(e => e.Subject)
                    .ThenInclude(s => s.Level)
                .Include(e => e.Teacher)
                .AsQueryable();

            if (userRole == "Student")
            {
                ViewBag.CurrentStudentId = userId;
                ViewBag.IsStudentUser = true;
                query = query.Where(e => e.StudentId == userId);

                var currentStudent = await _context.Student
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec.Level)
                    .FirstOrDefaultAsync(s => s.StudentId == userId);

                ViewBag.StudentId = new SelectList(new[] { currentStudent }, "StudentId", "FullName", userId);
                ViewBag.CurrentSectionId = currentStudent?.SectionId;
            }
            else
            {
                ViewBag.CurrentStudentId = null;
                ViewBag.IsStudentUser = false;

                // Apply filters for Admin/Super Admin
                if (levelId.HasValue)
                {
                    query = query.Where(e => e.Student.Section.LevelId == levelId.Value);
                }
                if (sectionId.HasValue)
                {
                    query = query.Where(e => e.Student.SectionId == sectionId.Value);
                }

                ViewBag.StudentId = new SelectList(await _context.Student
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec.Level)
                    .OrderBy(s => s.FullName)
                    .ToListAsync(), "StudentId", "FullName");
            }

            var enrollments = await query
                .OrderBy(e => e.Student.Section.Level.LevelName)
                .ThenBy(e => e.Student.Section.SectionName)
                .ThenBy(e => e.Student.FullName)
                .ToListAsync();

            ViewBag.SubjectId = new SelectList(await _context.Subject.ToListAsync(), "SubjectId", "SubjectName");

            return View(enrollments);
        }

        // GET: Enrollments/GetSectionsByLevel - NEW METHOD
        [HttpGet]
        public async Task<IActionResult> GetSectionsByLevel(int levelId)
        {
            if (levelId <= 0)
                return Json(new { success = false, message = "Level ID is required" });

            var sections = await _context.Section
                .Where(s => s.LevelId == levelId)
                .OrderBy(s => s.SectionName)
                .Select(s => new
                {
                    sectionId = s.SectionId,
                    sectionName = s.SectionName,
                    levelId = s.LevelId
                })
                .ToListAsync();

            return Json(new { success = true, data = sections });
        }

        // GET: Enrollments/GetSubjectsByStudent - UPDATED
        [HttpGet]
        public async Task<IActionResult> GetSubjectsByStudent(int studentId, int? excludeEnrollmentId = null, bool showEnrolled = false)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Security: Students can only query their own subjects
            if (userRole == "Student" && studentId != userId)
            {
                return Forbid();
            }

            var student = await _context.Student
                .Include(s => s.Section)
                    .ThenInclude(sec => sec.Level)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return Json(new { success = false, message = "Student not found" });

            // Get already enrolled subject IDs for this student
            var enrolledSubjectIds = await _context.Enrollment
                .Where(e => e.StudentId == studentId &&
                           (!excludeEnrollmentId.HasValue || e.EnrollmentId != excludeEnrollmentId.Value))
                .Select(e => e.SubjectId)
                .Distinct()
                .ToListAsync();

            // Get subjects that match the student's LEVEL (not section, for flexibility)
            // But you can change to match Section if subjects are section-specific
            var allSubjects = await _context.Subject
                .Include(s => s.Teacher)
                .Include(s => s.Level)
                .Where(s => s.LevelId == student.Section.LevelId && s.TeacherId != null && s.TeacherId > 0)
                .ToListAsync();

            var subjects = allSubjects.Select(s => new
            {
                subjectId = s.SubjectId,
                subjectName = s.SubjectName,
                teacherId = s.TeacherId,
                teacherName = s.Teacher != null ? s.Teacher.FullName : "No Teacher Assigned",
                level = s.Level != null ? s.Level.LevelName : "",
                levelId = s.LevelId,
                isEnrolled = enrolledSubjectIds.Contains(s.SubjectId)
            })
            .Where(s => showEnrolled || !s.isEnrolled)
            .ToList();

            return Json(new
            {
                success = true,
                data = subjects,
                studentLevel = student.Section?.Level?.LevelName ?? "",
                studentSection = student.Section?.SectionName ?? "",
                studentLevelId = student.Section?.LevelId,
                studentSectionId = student.SectionId,
                availableCount = subjects.Count(s => !s.isEnrolled),
                enrolledCount = enrolledSubjectIds.Count
            });
        }

        // POST: Enrollments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EnrollmentCreateViewModel model)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Security: Students can only enroll themselves
            if (userRole == "Student")
            {
                if (model.StudentId != userId)
                {
                    TempData["Error"] = "You can only enroll yourself!";
                    return RedirectToAction(nameof(Index));
                }
            }

            if (ModelState.IsValid)
            {
                var student = await _context.Student
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec.Level)
                    .FirstOrDefaultAsync(s => s.StudentId == model.StudentId);

                if (student == null)
                {
                    ModelState.AddModelError("", "Student not found");
                    await PopulateViewBagForCreate(userRole, model.StudentId);
                    return View(model);
                }

                int enrolledCount = 0;
                int skippedCount = 0;
                List<string> errors = new List<string>();

                foreach (var subjectId in model.SubjectIds)
                {
                    var subject = await _context.Subject
                        .Include(s => s.Level)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.SubjectId == subjectId);

                    if (subject == null)
                    {
                        errors.Add($"Subject ID {subjectId} not found");
                        continue;
                    }

                    // Security: Ensure subject level matches student's section level
                    if (subject.LevelId != student.Section.LevelId)
                    {
                        errors.Add($"Cannot enroll in '{subject.SubjectName}' - it's for {subject.Level?.LevelName ?? "Unknown"}, but student is in {student.Section?.Level?.LevelName ?? "Unknown"}");
                        continue;
                    }

                    // Prevent duplicate enrollment
                    if (await _context.Enrollment.AnyAsync(e => e.StudentId == model.StudentId && e.SubjectId == subjectId))
                    {
                        skippedCount++;
                        continue;
                    }

                    // Check if subject has a teacher assigned
                    if (subject.TeacherId == null || subject.TeacherId <= 0)
                    {
                        errors.Add($"Subject '{subject.SubjectName}' has no assigned teacher");
                        continue;
                    }

                    var enrollment = new Enrollment
                    {
                        StudentId = model.StudentId,
                        SubjectId = subjectId,
                        TeacherId = subject.TeacherId
                    };

                    _context.Add(enrollment);
                    enrolledCount++;
                }

                if (enrolledCount > 0)
                {
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Successfully enrolled in {enrolledCount} subject(s)!";

                    if (skippedCount > 0)
                        TempData["Info"] = $"{skippedCount} subject(s) were skipped (already enrolled)";

                    if (errors.Any())
                        TempData["Warning"] = string.Join("; ", errors);
                }
                else
                {
                    TempData["Error"] = errors.Any() ? string.Join("; ", errors) : "No subjects were enrolled";
                }

                return RedirectToAction(nameof(Index));
            }

            await PopulateViewBagForCreate(userRole, model.StudentId);
            return View(model);
        }

        // DELETE: Enrollments/Delete
        [Authorize(Roles = "Admin,Super Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var enrollment = await _context.Enrollment.FindAsync(id);
            if (enrollment != null)
            {
                _context.Enrollment.Remove(enrollment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Enrollment deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Enrollment not found!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EnrollmentExists(int id)
        {
            return _context.Enrollment.Any(e => e.EnrollmentId == id);
        }

        // Helper methods
        private async Task PopulateViewBagForCreate(string userRole, int? studentId = null)
        {
            if (userRole == "Student")
            {
                var currentStudent = await _context.Student
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec.Level)
                    .FirstOrDefaultAsync(s => s.StudentId == studentId);

                if (currentStudent != null)
                {
                    ViewBag.StudentId = new SelectList(new[] { currentStudent }, "StudentId", "FullName", currentStudent.StudentId);
                    ViewBag.IsStudentUser = true;
                }
            }
            else
            {
                ViewBag.StudentId = new SelectList(
                    await _context.Student
                        .Include(s => s.Section)
                            .ThenInclude(sec => sec.Level)
                        .OrderBy(s => s.FullName)
                        .ToListAsync(),
                    "StudentId", "FullName", studentId);
                ViewBag.IsStudentUser = false;
            }

            ViewBag.SubjectId = new SelectList(Enumerable.Empty<SelectListItem>());
        }
    }
}