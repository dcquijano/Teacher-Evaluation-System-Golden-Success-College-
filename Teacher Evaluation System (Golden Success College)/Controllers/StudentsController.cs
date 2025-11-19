using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    public class StudentsController : Controller
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public StudentsController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: Students
        public async Task<IActionResult> Index()
        {
            var teacher_Evaluation_System__Golden_Success_College_Context = _context.Student.Include(s => s.Level).Include(s => s.Role).Include(s => s.Section);
            return View(await teacher_Evaluation_System__Golden_Success_College_Context.ToListAsync());
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Student
                .Include(s => s.Level)
                .Include(s => s.Role)
                .Include(s => s.Section)
                .FirstOrDefaultAsync(m => m.StudentId == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            ViewData["LevelId"] = new SelectList(_context.Level, "LevelId", "LevelName");
            ViewData["RoleId"] = new SelectList(_context.Role, "RoleId", "Name");
            ViewData["SectionId"] = new SelectList(_context.Section, "SectionId", "SectionName");
            return View();
        }

        // POST: Students/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentId,FullName,Email,Password,LevelId,SectionId,CollegeYearLevel,RoleId")] Student student)
        {
            if (ModelState.IsValid)
            {
                // Set default role
                student.RoleId = 1; // Student

                // Set CollegeYearLevel automatically
                var level = await _context.Level.FindAsync(student.LevelId);
                if (level != null && level.LevelName.ToLower().Contains("college"))
                {
                    // If College, use provided year if valid (1-4), otherwise default to 1
                    student.CollegeYearLevel = (student.CollegeYearLevel >= 1 && student.CollegeYearLevel <= 4)
                                                ? student.CollegeYearLevel
                                                : 1;
                }
                else
                {
                    // Not College → set CollegeYearLevel to 0
                    student.CollegeYearLevel = 0;
                }

                _context.Add(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Repopulate dropdowns if model state is invalid
            ViewData["LevelId"] = new SelectList(_context.Level, "LevelId", "LevelName", student.LevelId);
            ViewData["SectionId"] = new SelectList(_context.Section, "SectionId", "SectionName", student.SectionId);
            ViewData["RoleId"] = new SelectList(_context.Role, "RoleId", "Name", student.RoleId);

            return View(student);
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Student.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            ViewData["LevelId"] = new SelectList(_context.Level, "LevelId", "LevelName", student.LevelId);
            ViewData["RoleId"] = new SelectList(_context.Role, "RoleId", "Name", student.RoleId);
            ViewData["SectionId"] = new SelectList(_context.Section, "SectionId", "SectionName", student.SectionId);
            return View(student);
        }

        // POST: Students/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StudentId,FullName,Email,Password,LevelId,SectionId,CollegeYearLevel,RoleId")] Student student)
        {
            if (id != student.StudentId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Set default role to Student
                    student.RoleId = 1;

                    // Auto-set CollegeYearLevel
                    var level = await _context.Level.FindAsync(student.LevelId);
                    if (level != null && level.LevelName.ToLower().Contains("college"))
                    {
                        student.CollegeYearLevel = (student.CollegeYearLevel >= 1 && student.CollegeYearLevel <= 4)
                                                    ? student.CollegeYearLevel
                                                    : 1;
                    }
                    else
                    {
                        student.CollegeYearLevel = 0; // None
                    }

                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.StudentId))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            // Repopulate dropdowns if model state is invalid
            ViewData["LevelId"] = new SelectList(_context.Level, "LevelId", "LevelName", student.LevelId);
            ViewData["SectionId"] = new SelectList(_context.Section, "SectionId", "SectionName", student.SectionId);
            ViewData["RoleId"] = new SelectList(_context.Role, "RoleId", "Name", student.RoleId);

            return View(student);
        }


        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Student
                .Include(s => s.Level)
                .Include(s => s.Role)
                .Include(s => s.Section)
                .FirstOrDefaultAsync(m => m.StudentId == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Student.FindAsync(id);
            if (student != null)
            {
                _context.Student.Remove(student);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Student.Any(e => e.StudentId == id);
        }
    }
}
