using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;
using Teacher_Evaluation_System__Golden_Success_College_.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Super Admin,Student")]
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
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var query = _context.Enrollment
                .Include(e => e.Student)
                .Include(e => e.Subject)
                .Include(e => e.Teacher)
                .AsQueryable();

            // Students can only see their own enrollments
            if (userRole == "Student")
            {
                var studentId = int.Parse(userId);
                query = query.Where(e => e.StudentId == studentId);
            }

            var enrollments = await query.ToListAsync();

            var data = enrollments.Select(e => new
            {
                enrollmentId = e.EnrollmentId,
                studentId = e.StudentId,
                studentName = e.Student?.FullName,
                subjectId = e.SubjectId,
                subjectName = e.Subject?.SubjectName,
                teacherId = e.TeacherId,
                teacherName = e.Teacher?.FullName
            });

            return Ok(new { success = true, data });
        }

        // GET: api/EnrollmentsApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEnrollment(int id)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var enrollment = await _context.Enrollment
                .Include(x => x.Student)
                .Include(x => x.Subject)
                .Include(x => x.Teacher)
                .FirstOrDefaultAsync(x => x.EnrollmentId == id);

            if (enrollment == null)
                return NotFound(new { success = false, message = "Enrollment not found" });

            // Students can only view their own enrollments
            if (userRole == "Student")
            {
                var studentId = int.Parse(userId);
                if (enrollment.StudentId != studentId)
                    return Forbid();
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    enrollmentId = enrollment.EnrollmentId,
                    studentId = enrollment.StudentId,
                    subjectId = enrollment.SubjectId,
                    teacherId = enrollment.TeacherId
                }
            });
        }

        // POST: api/EnrollmentsApi
        [HttpPost]
        public async Task<IActionResult> PostEnrollment([FromBody] EnrollmentCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { success = false, message = "User not authenticated" });

            // Security: Students can only enroll themselves
            if (userRole == "Student")
            {
                var studentId = int.Parse(userId);
                if (model.StudentId != studentId)
                {
                    return StatusCode(403, new { success = false, message = "You can only enroll yourself" });
                }
            }

            var student = await _context.Student
                .Include(s => s.Level)
                .FirstOrDefaultAsync(s => s.StudentId == model.StudentId);

            if (student == null)
                return BadRequest(new { success = false, message = "Student not found" });

            var createdEnrollments = new List<Enrollment>();
            var errors = new List<string>();
            var skippedCount = 0;

            foreach (var subId in model.SubjectIds)
            {
                var subject = await _context.Subject
                    .Include(s => s.Level)
                    .FirstOrDefaultAsync(s => s.SubjectId == subId);

                // Subject not found
                if (subject == null)
                {
                    errors.Add($"Subject with ID {subId} not found.");
                    continue;
                }

                // Duplicate enrollment
                if (await _context.Enrollment.AnyAsync(e => e.StudentId == model.StudentId && e.SubjectId == subId))
                {
                    skippedCount++;
                    errors.Add($"Already enrolled in {subject.SubjectName}.");
                    continue;
                }

                // Level mismatch
                if (subject.LevelId != student.LevelId)
                {
                    errors.Add($"Cannot enroll in '{subject.SubjectName}' - level mismatch (Subject: {subject.Level?.LevelName}, Student: {student.Level?.LevelName})");
                    continue;
                }

                // Missing teacher
                if (subject.TeacherId <= 0)
                {
                    errors.Add($"Subject '{subject.SubjectName}' has no assigned teacher.");
                    continue;
                }

                // Add valid enrollment
                createdEnrollments.Add(new Enrollment
                {
                    StudentId = model.StudentId,
                    SubjectId = subId,
                    TeacherId = subject.TeacherId
                });
            }


            if (!createdEnrollments.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No subjects enrolled",
                    errors = errors.Any() ? errors : new List<string> { "All subjects were already enrolled or invalid" }
                });
            }

            _context.Enrollment.AddRange(createdEnrollments);
            await _context.SaveChangesAsync();

            var responseMessage = $"Successfully enrolled in {createdEnrollments.Count} subject(s)";
            if (skippedCount > 0)
                responseMessage += $" ({skippedCount} skipped)";

            return Ok(new
            {
                success = true,
                message = responseMessage,
                enrolledCount = createdEnrollments.Count,
                skippedCount = skippedCount,
                errors = errors.Any() ? errors : null
            });
        }

        // PUT: api/EnrollmentsApi/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Super Admin")]
        public async Task<IActionResult> PutEnrollment(int id, [FromBody] Enrollment enrollment)
        {
            if (id != enrollment.EnrollmentId)
                return BadRequest(new { success = false, message = "Enrollment ID mismatch" });

            var existing = await _context.Enrollment
                .Include(e => e.Student)
                    .ThenInclude(s => s.Level)
                .FirstOrDefaultAsync(e => e.EnrollmentId == id);

            if (existing == null)
                return NotFound(new { success = false, message = "Enrollment not found" });

            // Validate student exists
            var student = await _context.Student
                .Include(s => s.Level)
                .FirstOrDefaultAsync(s => s.StudentId == enrollment.StudentId);

            if (student == null)
                return BadRequest(new { success = false, message = "Student not found" });

            // Validate subject exists and level matches
            var subject = await _context.Subject
                .Include(s => s.Level)
                .FirstOrDefaultAsync(s => s.SubjectId == enrollment.SubjectId);

            if (subject == null)
                return BadRequest(new { success = false, message = "Subject not found" });

            if (subject.LevelId != student.LevelId)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Level mismatch - Subject is for {subject.Level?.LevelName}, but student is in {student.Level?.LevelName}"
                });
            }

            // Validate teacher exists and level matches
            var teacher = await _context.Teacher
                .Include(t => t.Level)
                .FirstOrDefaultAsync(t => t.TeacherId == enrollment.TeacherId);

            if (teacher == null)
                return BadRequest(new { success = false, message = "Teacher not found" });

            if (teacher.LevelId != student.LevelId)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Level mismatch - Teacher teaches {teacher.Level?.LevelName}, but student is in {student.Level?.LevelName}"
                });
            }

            // Check for duplicate (excluding current record)
            var duplicateExists = await _context.Enrollment
                .AnyAsync(e => e.StudentId == enrollment.StudentId
                            && e.SubjectId == enrollment.SubjectId
                            && e.EnrollmentId != enrollment.EnrollmentId);

            if (duplicateExists)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "This student is already enrolled in this subject"
                });
            }

            // Update enrollment
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
                    return NotFound(new { success = false, message = "Enrollment no longer exists" });
                else
                    throw;
            }

            return Ok(new { success = true, message = "Enrollment updated successfully" });
        }

        // DELETE: api/EnrollmentsApi/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Super Admin")]
        public async Task<IActionResult> DeleteEnrollment(int id)
        {
            var enrollment = await _context.Enrollment.FindAsync(id);

            if (enrollment == null)
                return NotFound(new { success = false, message = "Enrollment not found" });

            _context.Enrollment.Remove(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Enrollment deleted successfully" });
        }

        private bool EnrollmentExists(int id)
        {
            return _context.Enrollment.Any(e => e.EnrollmentId == id);
        }
    }
}