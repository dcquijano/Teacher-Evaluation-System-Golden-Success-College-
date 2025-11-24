using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;
using Teacher_Evaluation_System__Golden_Success_College_.ViewModels;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    [Authorize(Roles = "Admin,Super Admin")]
    public class EnrollmentsController : Controller
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public EnrollmentsController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: Enrollments
        public async Task<IActionResult> Index()
        {
            var enrollments = await _context.Enrollment
                .Include(e => e.Student)
                    .ThenInclude(s => s.Level)
                .Include(e => e.Subject)
                    .ThenInclude(s => s.Level)
                .Include(e => e.Teacher)
                    .ThenInclude(t => t.Level)
                .ToListAsync();

            ViewBag.StudentId = new SelectList(await _context.Student.ToListAsync(), "StudentId", "FullName");
            ViewBag.SubjectId = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewBag.TeacherId = new SelectList(Enumerable.Empty<SelectListItem>());

            return View(enrollments);
        }

        // GET: Enrollments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollment
                .Include(e => e.Student)
                    .ThenInclude(s => s.Level)
                .Include(e => e.Subject)
                    .ThenInclude(s => s.Level)
                .Include(e => e.Teacher)
                    .ThenInclude(t => t.Level)
                .FirstOrDefaultAsync(m => m.EnrollmentId == id);

            if (enrollment == null)
            {
                return NotFound();
            }

            return View(enrollment);
        }

        // GET: Enrollments/Create
        public IActionResult Create()
        {
            ViewBag.StudentId = new SelectList(_context.Student, "StudentId", "FullName");
            ViewBag.SubjectId = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewBag.SubjectTeacher = new Dictionary<int, string>();

            return View();
        }

        // GET: Enrollments/GetSubjectsByStudent
        [HttpGet]
        public async Task<IActionResult> GetSubjectsByStudent(int studentId)
        {
            var student = await _context.Student
                .Include(s => s.Level)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return Json(new { success = false, message = "Student not found" });

            // Get subjects that match the student's level (by LevelId)
            var subjects = await _context.Subject
                .Include(s => s.Teacher)
                .Include(s => s.Level)
                .Where(s => s.LevelId == student.LevelId)
                .Select(s => new
                {
                    subjectId = s.SubjectId,
                    subjectName = s.SubjectName,
                    teacherId = s.TeacherId,
                    teacherName = s.Teacher != null ? s.Teacher.FullName : "No Teacher Assigned",
                    level = s.Level != null ? s.Level.LevelName : "",
                    levelId = s.LevelId
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                data = subjects,
                studentLevel = student.Level?.LevelName ?? "",
                studentLevelId = student.LevelId
            });
        }

        // GET: Enrollments/GetTeachersByLevel
        [HttpGet]
        public async Task<IActionResult> GetTeachersByLevel(int levelId)
        {
            if (levelId <= 0)
                return Json(new { success = false, message = "Level ID is required" });

            var teachers = await _context.Teacher
                .Include(t => t.Level)
                .Where(t => t.LevelId == levelId)
                .Select(t => new
                {
                    teacherId = t.TeacherId,
                    teacherName = t.FullName,
                    level = t.Level != null ? t.Level.LevelName : ""
                })
                .ToListAsync();

            return Json(new { success = true, data = teachers });
        }

        // POST: Enrollments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EnrollmentCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var student = await _context.Student
                    .Include(s => s.Level)
                    .FirstOrDefaultAsync(s => s.StudentId == model.StudentId);

                if (student == null)
                {
                    ModelState.AddModelError("", "Student not found");
                    ViewBag.StudentId = new SelectList(_context.Student, "StudentId", "FullName", model.StudentId);
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

                    // Security check: Ensure subject level matches student level (by LevelId)
                    if (subject.LevelId != student.LevelId)
                    {
                        errors.Add($"Cannot enroll in '{subject.SubjectName}' - it's for {subject.Level?.LevelName ?? "Unknown"}, but student is in {student.Level?.LevelName ?? "Unknown"}");
                        continue;
                    }

                    // Prevent duplicate enrollment
                    if (_context.Enrollment.Any(e => e.StudentId == model.StudentId && e.SubjectId == subjectId))
                    {
                        skippedCount++;
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

            ViewBag.StudentId = new SelectList(_context.Student, "StudentId", "FullName", model.StudentId);
            return View(model);
        }

        // GET: Enrollments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollment
                .Include(e => e.Student)
                    .ThenInclude(s => s.Level)
                .Include(e => e.Subject)
                    .ThenInclude(s => s.Level)
                .Include(e => e.Teacher)
                    .ThenInclude(t => t.Level)
                .FirstOrDefaultAsync(e => e.EnrollmentId == id);

            if (enrollment == null)
                return NotFound();

            // Get student level to filter subjects and teachers
            var studentLevelId = enrollment.Student?.LevelId;

            ViewData["StudentId"] = new SelectList(_context.Student, "StudentId", "FullName", enrollment.StudentId);

            // Filter subjects by student level
            var subjects = await _context.Subject
                .Where(s => s.LevelId == studentLevelId)
                .ToListAsync();
            ViewData["SubjectId"] = new SelectList(subjects, "SubjectId", "SubjectName", enrollment.SubjectId);

            // Filter teachers by student level
            var teachers = await _context.Teacher
                .Where(t => t.LevelId == studentLevelId)
                .ToListAsync();
            ViewData["TeacherId"] = new SelectList(teachers, "TeacherId", "FullName", enrollment.TeacherId);

            return View(enrollment);
        }

        // POST: Enrollments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EnrollmentId,StudentId,SubjectId,TeacherId")] Enrollment enrollment)
        {
            if (id != enrollment.EnrollmentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var student = await _context.Student
                        .Include(s => s.Level)
                        .FirstOrDefaultAsync(s => s.StudentId == enrollment.StudentId);

                    if (student == null)
                    {
                        ModelState.AddModelError("", "Student not found");
                        return View(enrollment);
                    }

                    var subject = await _context.Subject
                        .Include(s => s.Level)
                        .FirstOrDefaultAsync(s => s.SubjectId == enrollment.SubjectId);

                    if (subject == null)
                    {
                        ModelState.AddModelError("", "Subject not found");
                        return View(enrollment);
                    }

                    // Verify subject level matches student level (by LevelId)
                    if (subject.LevelId != student.LevelId)
                    {
                        ModelState.AddModelError("SubjectId", $"Cannot assign this subject - it's for {subject.Level?.LevelName ?? "Unknown"}, but student is in {student.Level?.LevelName ?? "Unknown"}");

                        var subjects = await _context.Subject
                            .Where(s => s.LevelId == student.LevelId)
                            .ToListAsync();
                        ViewData["SubjectId"] = new SelectList(subjects, "SubjectId", "SubjectName", enrollment.SubjectId);
                        ViewData["StudentId"] = new SelectList(_context.Student, "StudentId", "FullName", enrollment.StudentId);

                        var teachers = await _context.Teacher
                            .Where(t => t.LevelId == student.LevelId)
                            .ToListAsync();
                        ViewData["TeacherId"] = new SelectList(teachers, "TeacherId", "FullName", enrollment.TeacherId);

                        return View(enrollment);
                    }

                    var teacher = await _context.Teacher
                        .Include(t => t.Level)
                        .FirstOrDefaultAsync(t => t.TeacherId == enrollment.TeacherId);

                    if (teacher == null)
                    {
                        ModelState.AddModelError("", "Teacher not found");
                        return View(enrollment);
                    }

                    // Verify teacher level matches student level (by LevelId)
                    if (teacher.LevelId != student.LevelId)
                    {
                        ModelState.AddModelError("TeacherId", $"Cannot assign this teacher - they teach {teacher.Level?.LevelName ?? "Unknown"}, but student is in {student.Level?.LevelName ?? "Unknown"}");

                        var subjects = await _context.Subject
                            .Where(s => s.LevelId == student.LevelId)
                            .ToListAsync();
                        ViewData["SubjectId"] = new SelectList(subjects, "SubjectId", "SubjectName", enrollment.SubjectId);
                        ViewData["StudentId"] = new SelectList(_context.Student, "StudentId", "FullName", enrollment.StudentId);

                        var teachers = await _context.Teacher
                            .Where(t => t.LevelId == student.LevelId)
                            .ToListAsync();
                        ViewData["TeacherId"] = new SelectList(teachers, "TeacherId", "FullName", enrollment.TeacherId);

                        return View(enrollment);
                    }

                    // Check for duplicate enrollment (excluding current record)
                    var duplicateExists = await _context.Enrollment
                        .AnyAsync(e => e.StudentId == enrollment.StudentId
                                    && e.SubjectId == enrollment.SubjectId
                                    && e.EnrollmentId != enrollment.EnrollmentId);

                    if (duplicateExists)
                    {
                        ModelState.AddModelError("", "This student is already enrolled in this subject");

                        var subjects = await _context.Subject
                            .Where(s => s.LevelId == student.LevelId)
                            .ToListAsync();
                        ViewData["SubjectId"] = new SelectList(subjects, "SubjectId", "SubjectName", enrollment.SubjectId);
                        ViewData["StudentId"] = new SelectList(_context.Student, "StudentId", "FullName", enrollment.StudentId);

                        var teachers = await _context.Teacher
                            .Where(t => t.LevelId == student.LevelId)
                            .ToListAsync();
                        ViewData["TeacherId"] = new SelectList(teachers, "TeacherId", "FullName", enrollment.TeacherId);

                        return View(enrollment);
                    }

                    _context.Update(enrollment);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Enrollment updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EnrollmentExists(enrollment.EnrollmentId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["StudentId"] = new SelectList(_context.Student, "StudentId", "FullName", enrollment.StudentId);
            ViewData["SubjectId"] = new SelectList(_context.Subject, "SubjectId", "SubjectName", enrollment.SubjectId);
            ViewData["TeacherId"] = new SelectList(_context.Teacher, "TeacherId", "FullName", enrollment.TeacherId);

            return View(enrollment);
        }

        // GET: Enrollments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollment
                .Include(e => e.Student)
                    .ThenInclude(s => s.Level)
                .Include(e => e.Subject)
                    .ThenInclude(s => s.Level)
                .Include(e => e.Teacher)
                    .ThenInclude(t => t.Level)
                .FirstOrDefaultAsync(m => m.EnrollmentId == id);

            if (enrollment == null)
            {
                return NotFound();
            }

            return View(enrollment);
        }

        // POST: Enrollments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
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
    }
}