using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;
using Teacher_Evaluation_System__Golden_Success_College_.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentsApiController : ControllerBase
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public EnrollmentsApiController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: api/EnrollmentsApi
        [HttpGet]
        public async Task<IActionResult> GetEnrollments()
        {
            var enrollments = await _context.Enrollment
                .Include(e => e.Student)
                    .ThenInclude(s => s.Level)
                .Include(e => e.Subject)
                    .ThenInclude(s => s.Level)
                .Include(e => e.Teacher)
                    .ThenInclude(t => t.Level)
                .ToListAsync();

            var data = enrollments.Select(e => new
            {
                enrollmentId = e.EnrollmentId,
                studentId = e.StudentId,
                studentName = e.Student?.FullName,
                subjectId = e.SubjectId,
                subjectName = e.Subject?.SubjectName,
                teacherId = e.TeacherId,
                teacherName = e.Teacher?.FullName,
                studentLevel = e.Student?.Level?.LevelName ?? "",
                subjectLevel = e.Subject?.Level?.LevelName ?? ""
            });

            return Ok(new
            {
                success = true,
                data = data
            });
        }

        // GET: api/EnrollmentsApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEnrollment(int id)
        {
            var enrollment = await _context.Enrollment
                .Include(e => e.Student)
                    .ThenInclude(s => s.Level)
                .Include(e => e.Subject)
                    .ThenInclude(s => s.Level)
                .Include(e => e.Teacher)
                    .ThenInclude(t => t.Level)
                .FirstOrDefaultAsync(e => e.EnrollmentId == id);

            if (enrollment == null)
                return NotFound(new { success = false, message = "Enrollment not found" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    enrollmentId = enrollment.EnrollmentId,
                    studentId = enrollment.StudentId,
                    studentName = enrollment.Student?.FullName,
                    studentLevel = enrollment.Student?.Level?.LevelName ?? "",
                    subjectId = enrollment.SubjectId,
                    subjectName = enrollment.Subject?.SubjectName,
                    subjectLevel = enrollment.Subject?.Level?.LevelName ?? "",
                    teacherId = enrollment.TeacherId,
                    teacherName = enrollment.Teacher?.FullName,
                    teacherLevel = enrollment.Teacher?.Level?.LevelName ?? ""
                }
            });
        }

        // GET: api/EnrollmentsApi/subjects-by-student/{studentId}
        [HttpGet("subjects-by-student/{studentId}")]
        public async Task<IActionResult> GetSubjectsByStudent(int studentId)
        {
            var student = await _context.Student
                .Include(s => s.Level)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return NotFound(new { success = false, message = "Student not found" });

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
                    level = s.Level != null ? s.Level.LevelName : ""
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = subjects,
                studentLevel = student.Level?.LevelName ?? ""
            });
        }

        // GET: api/EnrollmentsApi/teachers-by-level/{levelId}
        [HttpGet("teachers-by-level/{levelId}")]
        public async Task<IActionResult> GetTeachersByLevel(int levelId)
        {
            if (levelId <= 0)
                return BadRequest(new { success = false, message = "Level ID is required" });

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

            return Ok(new { success = true, data = teachers });
        }

        // POST: api/EnrollmentsApi
        [HttpPost]
        public async Task<IActionResult> PostEnrollment([FromBody] EnrollmentCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data" });

            // Verify student exists and get their level
            var student = await _context.Student
                .Include(s => s.Level)
                .FirstOrDefaultAsync(s => s.StudentId == model.StudentId);

            if (student == null)
                return BadRequest(new { success = false, message = "Student not found" });

            var createdEnrollments = new List<Enrollment>();
            var errors = new List<string>();
            var skippedCount = 0;

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

                // CRITICAL: Verify subject level matches student level (by LevelId)
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

                _context.Enrollment.Add(enrollment);
                createdEnrollments.Add(enrollment);
            }

            if (createdEnrollments.Count == 0)
            {
                var message = errors.Any() ? string.Join("; ", errors) : "No subjects were enrolled (may already be enrolled)";
                return BadRequest(new { success = false, message = message });
            }

            await _context.SaveChangesAsync();

            // Reload entities to get navigation properties
            foreach (var e in createdEnrollments)
            {
                await _context.Entry(e).Reference(x => x.Student).LoadAsync();
                await _context.Entry(e).Reference(x => x.Subject).LoadAsync();
                await _context.Entry(e).Reference(x => x.Teacher).LoadAsync();
            }

            var data = createdEnrollments.Select(e => new
            {
                enrollmentId = e.EnrollmentId,
                studentId = e.StudentId,
                studentName = e.Student?.FullName,
                subjectId = e.SubjectId,
                subjectName = e.Subject?.SubjectName,
                teacherId = e.TeacherId,
                teacherName = e.Teacher?.FullName
            });

            var successMessage = $"Successfully enrolled in {createdEnrollments.Count} subject(s)";
            if (skippedCount > 0)
                successMessage += $" ({skippedCount} already enrolled)";
            if (errors.Any())
                successMessage += $". Warnings: {string.Join("; ", errors)}";

            return Ok(new
            {
                success = true,
                message = successMessage,
                data = data
            });
        }

        // PUT: api/EnrollmentsApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEnrollment(int id, [FromBody] Enrollment enrollment)
        {
            if (id != enrollment.EnrollmentId)
                return BadRequest(new { success = false, message = "Enrollment ID mismatch" });

            var existing = await _context.Enrollment.FindAsync(id);
            if (existing == null)
                return NotFound(new { success = false, message = "Enrollment not found" });

            // Get student to check level
            var student = await _context.Student
                .Include(s => s.Level)
                .FirstOrDefaultAsync(s => s.StudentId == enrollment.StudentId);

            if (student == null)
                return BadRequest(new { success = false, message = "Student not found" });

            // Get subject to check level
            var subject = await _context.Subject
                .Include(s => s.Level)
                .FirstOrDefaultAsync(s => s.SubjectId == enrollment.SubjectId);

            if (subject == null)
                return BadRequest(new { success = false, message = "Subject not found" });

            // Verify subject level matches student level (by LevelId)
            if (subject.LevelId != student.LevelId)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Cannot assign this subject - it's for {subject.Level?.LevelName ?? "Unknown"}, but student is in {student.Level?.LevelName ?? "Unknown"}"
                });
            }

            // Get teacher to check level
            var teacher = await _context.Teacher
                .Include(t => t.Level)
                .FirstOrDefaultAsync(t => t.TeacherId == enrollment.TeacherId);

            if (teacher == null)
                return BadRequest(new { success = false, message = "Teacher not found" });

            // Verify teacher level matches student level (by LevelId)
            if (teacher.LevelId != student.LevelId)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Cannot assign this teacher - they teach {teacher.Level?.LevelName ?? "Unknown"}, but student is in {student.Level?.LevelName ?? "Unknown"}"
                });
            }

            // Check for duplicate enrollment (excluding current record)
            var duplicateExists = await _context.Enrollment
                .AnyAsync(e => e.StudentId == enrollment.StudentId
                            && e.SubjectId == enrollment.SubjectId
                            && e.EnrollmentId != enrollment.EnrollmentId);

            if (duplicateExists)
            {
                return BadRequest(new { success = false, message = "This student is already enrolled in this subject" });
            }

            existing.StudentId = enrollment.StudentId;
            existing.SubjectId = enrollment.SubjectId;
            existing.TeacherId = enrollment.TeacherId;

            _context.Entry(existing).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EnrollmentExists(id))
                {
                    return NotFound(new { success = false, message = "Enrollment not found" });
                }
                else
                {
                    throw;
                }
            }

            // Load navigation properties
            await _context.Entry(existing).Reference(x => x.Student).LoadAsync();
            await _context.Entry(existing).Reference(x => x.Subject).LoadAsync();
            await _context.Entry(existing).Reference(x => x.Teacher).LoadAsync();

            return Ok(new
            {
                success = true,
                message = "Enrollment updated successfully",
                data = new
                {
                    enrollmentId = existing.EnrollmentId,
                    studentId = existing.StudentId,
                    studentName = existing.Student?.FullName,
                    subjectId = existing.SubjectId,
                    subjectName = existing.Subject?.SubjectName,
                    teacherId = existing.TeacherId,
                    teacherName = existing.Teacher?.FullName
                }
            });
        }

        // DELETE: api/EnrollmentsApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEnrollment(int id)
        {
            var enrollment = await _context.Enrollment.FindAsync(id);
            if (enrollment == null)
                return NotFound(new { success = false, message = "Enrollment not found" });

            _context.Enrollment.Remove(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Enrollment deleted successfully"
            });
        }

        // Helper method to check if enrollment exists
        private bool EnrollmentExists(int id)
        {
            return _context.Enrollment.Any(e => e.EnrollmentId == id);
        }
    }
}