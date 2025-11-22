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

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    [Authorize(Roles = "Admin,Super Admin")]
    public class SubjectsController : Controller
    {

        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public SubjectsController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: Subjects
        public async Task<IActionResult> Index()
        {
            var subjects = await _context.Subject
                .Include(s => s.Level)
                .Include(s => s.Section)
                    .ThenInclude(sec => sec.Level)
                .Include(s => s.Teacher)
                .ToListAsync();

            // Load levels for dropdown
            ViewData["LevelId"] = new SelectList(await _context.Level.ToListAsync(), "LevelId", "LevelName");

            // Load sections with their Level for grouping
            var sections = await _context.Section.Include(s => s.Level).ToListAsync();

            // Convert to SelectListItem with Group (optgroup)
            var sectionSelectList = sections.Select(s => new SelectListItem
            {
                Value = s.SectionId.ToString(),
                Text = s.SectionName,
                Group = new SelectListGroup { Name = s.Level.LevelName } // College, Senior High, Junior High
            }).ToList();

            ViewData["SectionId"] = sectionSelectList;

            // Load teachers for dropdown
            ViewData["TeacherId"] = new SelectList(
                await _context.Teacher.ToListAsync(),
                "TeacherId",
                "FullName"
            );

            return View(subjects);
        }

        // GET: Subjects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subject = await _context.Subject
                .Include(s => s.Level)
                .Include(s => s.Section)
                    .ThenInclude(sec => sec.Level)
                .Include(s => s.Teacher)
                .FirstOrDefaultAsync(m => m.SubjectId == id);

            if (subject == null)
            {
                return NotFound();
            }

            return View(subject);
        }

        // GET: Subjects/Create
        public async Task<IActionResult> Create()
        {
            // Load levels for dropdown
            ViewData["LevelId"] = new SelectList(await _context.Level.ToListAsync(), "LevelId", "LevelName");

            // Load sections with their Level for grouping
            var sections = await _context.Section.Include(s => s.Level).ToListAsync();

            var sectionSelectList = sections.Select(s => new SelectListItem
            {
                Value = s.SectionId.ToString(),
                Text = s.SectionName,
                Group = new SelectListGroup { Name = s.Level.LevelName }
            }).ToList();

            ViewData["SectionId"] = sectionSelectList;
            ViewData["TeacherId"] = new SelectList(await _context.Teacher.ToListAsync(), "TeacherId", "FullName");

            return View();
        }

        // POST: Subjects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SubjectId,SubjectName,SubjectCode,LevelId,SectionId,TeacherId,Schedule")] Subject subject)
        {
            if (ModelState.IsValid)
            {
                _context.Add(subject);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Reload dropdowns
            ViewData["LevelId"] = new SelectList(await _context.Level.ToListAsync(), "LevelId", "LevelName", subject.LevelId);

            var sections = await _context.Section.Include(s => s.Level).ToListAsync();
            var sectionSelectList = sections.Select(s => new SelectListItem
            {
                Value = s.SectionId.ToString(),
                Text = s.SectionName,
                Group = new SelectListGroup { Name = s.Level.LevelName }
            }).ToList();

            ViewData["SectionId"] = sectionSelectList;
            ViewData["TeacherId"] = new SelectList(await _context.Teacher.ToListAsync(), "TeacherId", "FullName", subject.TeacherId);

            return View(subject);
        }

        // GET: Subjects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subject = await _context.Subject.FindAsync(id);
            if (subject == null)
            {
                return NotFound();
            }

            // Load levels
            ViewData["LevelId"] = new SelectList(await _context.Level.ToListAsync(), "LevelId", "LevelName", subject.LevelId);

            // Load sections with grouping
            var sections = await _context.Section.Include(s => s.Level).ToListAsync();
            var sectionSelectList = sections.Select(s => new SelectListItem
            {
                Value = s.SectionId.ToString(),
                Text = s.SectionName,
                Group = new SelectListGroup { Name = s.Level.LevelName }
            }).ToList();

            ViewData["SectionId"] = sectionSelectList;
            ViewData["TeacherId"] = new SelectList(await _context.Teacher.ToListAsync(), "TeacherId", "FullName", subject.TeacherId);

            return View(subject);
        }

        // POST: Subjects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SubjectId,SubjectName,SubjectCode,LevelId,SectionId,TeacherId,Schedule")] Subject subject)
        {
            if (id != subject.SubjectId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(subject);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubjectExists(subject.SubjectId))
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

            // Reload dropdowns
            ViewData["LevelId"] = new SelectList(await _context.Level.ToListAsync(), "LevelId", "LevelName", subject.LevelId);

            var sections = await _context.Section.Include(s => s.Level).ToListAsync();
            var sectionSelectList = sections.Select(s => new SelectListItem
            {
                Value = s.SectionId.ToString(),
                Text = s.SectionName,
                Group = new SelectListGroup { Name = s.Level.LevelName }
            }).ToList();

            ViewData["SectionId"] = sectionSelectList;
            ViewData["TeacherId"] = new SelectList(await _context.Teacher.ToListAsync(), "TeacherId", "FullName", subject.TeacherId);

            return View(subject);
        }

        // GET: Subjects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subject = await _context.Subject
                .Include(s => s.Level)
                .Include(s => s.Section)
                    .ThenInclude(sec => sec.Level)
                .Include(s => s.Teacher)
                .FirstOrDefaultAsync(m => m.SubjectId == id);

            if (subject == null)
            {
                return NotFound();
            }

            return View(subject);
        }

        // POST: Subjects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subject = await _context.Subject.FindAsync(id);
            if (subject != null)
            {
                _context.Subject.Remove(subject);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SubjectExists(int id)
        {
            return _context.Subject.Any(e => e.SubjectId == id);
        }
    }
}