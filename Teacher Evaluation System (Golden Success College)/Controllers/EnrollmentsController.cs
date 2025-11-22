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
                .Include(e => e.Subject)
                .Include(e => e.Teacher)
                .ToListAsync();

            ViewBag.StudentId = new SelectList(await _context.Student.ToListAsync(), "StudentId", "FullName");
            ViewBag.SubjectId = new SelectList(await _context.Subject.ToListAsync(), "SubjectId", "SubjectName");
            ViewBag.TeacherId = new SelectList(await _context.Teacher.ToListAsync(), "TeacherId", "FullName");

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
                .Include(e => e.Subject)
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

            // Prepare subjects with teacher names
            var subjects = _context.Subject
                .Include(s => s.Teacher)
                .Select(s => new SelectListItem
                {
                    Value = s.SubjectId.ToString(),
                    Text = s.SubjectName,
                    // Store teacher name in a custom Data property
                    // We'll use this in Razor with @item.DataTeacher
                    // Razor doesn't support custom properties, so we'll use a Dictionary
                }).ToList();

            // Prepare dictionary for teacher lookup
            var teacherDict = _context.Subject
                .Include(s => s.Teacher)
                .ToDictionary(s => s.SubjectId, s => s.Teacher.FullName);

            ViewBag.SubjectId = new SelectList(subjects, "Value", "Text");
            ViewBag.SubjectTeacher = teacherDict; // Pass the teacher names

            return View();
        }

        // POST: Enrollments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EnrollmentCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                foreach (var subjectId in model.SubjectIds)
                {
                    // Get the subject and its teacher
                    var subject = await _context.Subject
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.SubjectId == subjectId);

                    if (subject == null) continue; // Skip if subject not found

                    // Prevent duplicate enrollment
                    if (_context.Enrollment.Any(e => e.StudentId == model.StudentId && e.SubjectId == subjectId))
                        continue;

                    var enrollment = new Enrollment
                    {
                        StudentId = model.StudentId,
                        SubjectId = subjectId,
                        TeacherId = subject.TeacherId
                    };

                    _context.Add(enrollment);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Subjects assigned successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["StudentId"] = new SelectList(_context.Student, "StudentId", "FullName", model.StudentId);
            ViewData["SubjectId"] = new SelectList(_context.Subject, "SubjectId", "SubjectName");
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
              .Include(e => e.Subject)
              .Include(e => e.Teacher)
              .FirstOrDefaultAsync(e => e.EnrollmentId == id);

            if (enrollment == null)
                return NotFound();

            ViewData["StudentId"] = new SelectList(_context.Student, "StudentId", "FullName", enrollment.StudentId);
            ViewData["SubjectId"] = new SelectList(_context.Subject, "SubjectId", "SubjectName", enrollment.SubjectId);
            ViewData["TeacherId"] = new SelectList(_context.Teacher, "TeacherId", "FullName", enrollment.TeacherId);

            return View(enrollment);
        }

        // POST: Enrollments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
                    _context.Update(enrollment);
                    await _context.SaveChangesAsync();
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
                .Include(e => e.Subject)
                .Include(e => e.Teacher)
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
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EnrollmentExists(int id)
        {
            return _context.Enrollment.Any(e => e.EnrollmentId == id);
        }
    }
}
