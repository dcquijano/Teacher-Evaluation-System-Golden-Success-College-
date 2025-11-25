using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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
        public async Task<IActionResult> Index()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            ViewBag.UserRole = userRole;

            // Build query based on role
            var query = _context.Enrollment
                .Include(e => e.Student)
                    .ThenInclude(s => s.Level)
                .Include(e => e.Subject)
                    .ThenInclude(s => s.Level)
                .Include(e => e.Teacher)
                    .ThenInclude(t => t.Level)
                .AsQueryable();

            if (userRole == "Student")
            {
                ViewBag.CurrentStudentId = userId;
                ViewBag.IsStudentUser = true;
                query = query.Where(e => e.StudentId == userId);

                // Only show current student in dropdown
                var currentStudent = await _context.Student.FindAsync(userId);
                ViewBag.StudentId = new SelectList(new[] { currentStudent }, "StudentId", "FullName", userId);
            }
            else
            {
                ViewBag.CurrentStudentId = null;
                ViewBag.IsStudentUser = false;
                ViewBag.StudentId = new SelectList(await _context.Student.ToListAsync(), "StudentId", "FullName");
            }

            var enrollments = await query.OrderBy(e => e.Student.FullName).ToListAsync();

            // Subject list for reference
            ViewBag.SubjectId = new SelectList(await _context.Subject.ToListAsync(), "SubjectId", "SubjectName");

            return View(enrollments);
        }

        // GET: Enrollments/Create
        public async Task<IActionResult> Create()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Name);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // If student, auto-select their account
            if (userRole == "Student")
            {
                var currentStudent = await _context.Student
                    .FirstOrDefaultAsync(s => s.Email == userEmail);

                if (currentStudent != null)
                {
                    ViewBag.StudentId = new SelectList(new[] { currentStudent }, "StudentId", "FullName", currentStudent.StudentId);
                    ViewBag.IsStudentUser = true;
                    ViewBag.CurrentStudentId = currentStudent.StudentId;
                }
            }
            else
            {
                ViewBag.StudentId = new SelectList(_context.Student, "StudentId", "FullName");
                ViewBag.IsStudentUser = false;
            }

            ViewBag.SubjectId = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewBag.SubjectTeacher = new Dictionary<int, string>();

            return View();
        }

        // GET: Enrollments/GetSubjectsByStudent
        [HttpGet]
        public async Task<IActionResult> GetSubjectsByStudent(int studentId)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Security: Students can only query their own subjects
            if (userRole == "Student" && studentId != userId)
            {
                return Forbid();
            }

            var student = await _context.Student
                .Include(s => s.Level)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return Json(new { success = false, message = "Student not found" });

            // Get subjects that match the student's level
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
                    .Include(s => s.Level)
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

                    // Security: Ensure subject level matches student level
                    if (subject.LevelId != student.LevelId)
                    {
                        errors.Add($"Cannot enroll in '{subject.SubjectName}' - it's for {subject.Level?.LevelName ?? "Unknown"}, but student is in {student.Level?.LevelName ?? "Unknown"}");
                        continue;
                    }

                    // Prevent duplicate enrollment
                    if (await _context.Enrollment.AnyAsync(e => e.StudentId == model.StudentId && e.SubjectId == subjectId))
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

            await PopulateViewBagForCreate(userRole, model.StudentId);
            return View(model);
        }

        // GET: Enrollments/Edit/5
        [Authorize(Roles = "Admin,Super Admin")]
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
        [Authorize(Roles = "Admin,Super Admin")]
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
                        await PopulateViewDataForEdit(enrollment);
                        return View(enrollment);
                    }

                    var subject = await _context.Subject
                        .Include(s => s.Level)
                        .FirstOrDefaultAsync(s => s.SubjectId == enrollment.SubjectId);

                    if (subject == null)
                    {
                        ModelState.AddModelError("", "Subject not found");
                        await PopulateViewDataForEdit(enrollment);
                        return View(enrollment);
                    }

                    // Verify subject level matches student level
                    if (subject.LevelId != student.LevelId)
                    {
                        ModelState.AddModelError("SubjectId", $"Cannot assign this subject - it's for {subject.Level?.LevelName ?? "Unknown"}, but student is in {student.Level?.LevelName ?? "Unknown"}");
                        await PopulateViewDataForEdit(enrollment, student.LevelId);
                        return View(enrollment);
                    }

                    var teacher = await _context.Teacher
                        .Include(t => t.Level)
                        .FirstOrDefaultAsync(t => t.TeacherId == enrollment.TeacherId);

                    if (teacher == null)
                    {
                        ModelState.AddModelError("", "Teacher not found");
                        await PopulateViewDataForEdit(enrollment);
                        return View(enrollment);
                    }

                    // Verify teacher level matches student level
                    if (teacher.LevelId != student.LevelId)
                    {
                        ModelState.AddModelError("TeacherId", $"Cannot assign this teacher - they teach {teacher.Level?.LevelName ?? "Unknown"}, but student is in {student.Level?.LevelName ?? "Unknown"}");
                        await PopulateViewDataForEdit(enrollment, student.LevelId);
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
                        await PopulateViewDataForEdit(enrollment, student.LevelId);
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

            await PopulateViewDataForEdit(enrollment);
            return View(enrollment);
        }

        // GET: Enrollments/Delete/5
        [Authorize(Roles = "Admin,Super Admin")]
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
        [Authorize(Roles = "Admin,Super Admin")]
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

        // Helper methods
        private async Task PopulateViewBagForCreate(string userRole, int? studentId = null)
        {
            if (userRole == "Student")
            {
                var currentStudent = await _context.Student.FindAsync(studentId);
                if (currentStudent != null)
                {
                    ViewBag.StudentId = new SelectList(new[] { currentStudent }, "StudentId", "FullName", currentStudent.StudentId);
                    ViewBag.IsStudentUser = true;
                }
            }
            else
            {
                ViewBag.StudentId = new SelectList(_context.Student, "StudentId", "FullName", studentId);
                ViewBag.IsStudentUser = false;
            }

            ViewBag.SubjectId = new SelectList(Enumerable.Empty<SelectListItem>());
        }

        private async Task PopulateViewDataForEdit(Enrollment enrollment, int? levelId = null)
        {
            ViewData["StudentId"] = new SelectList(_context.Student, "StudentId", "FullName", enrollment.StudentId);

            if (levelId.HasValue)
            {
                var subjects = await _context.Subject
                    .Where(s => s.LevelId == levelId)
                    .ToListAsync();
                ViewData["SubjectId"] = new SelectList(subjects, "SubjectId", "SubjectName", enrollment.SubjectId);

                var teachers = await _context.Teacher
                    .Where(t => t.LevelId == levelId)
                    .ToListAsync();
                ViewData["TeacherId"] = new SelectList(teachers, "TeacherId", "FullName", enrollment.TeacherId);
            }
            else
            {
                ViewData["SubjectId"] = new SelectList(_context.Subject, "SubjectId", "SubjectName", enrollment.SubjectId);
                ViewData["TeacherId"] = new SelectList(_context.Teacher, "TeacherId", "FullName", enrollment.TeacherId);
            }
        }
    }
}