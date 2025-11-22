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
    public class TeachersController : Controller
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;
        private readonly IWebHostEnvironment _env;

        public TeachersController(Teacher_Evaluation_System__Golden_Success_College_Context context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Teachers
        public async Task<IActionResult> Index()
        {
            var teachers = await _context.Teacher
                 .Include(t => t.Level)
                 .ToListAsync();

            ViewBag.LevelId = new SelectList(await _context.Level.ToListAsync(), "LevelId", "LevelName");

            return View(teachers);
        }

        // GET: Teachers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var teacher = await _context.Teacher
                .Include(t => t.Level)
                .FirstOrDefaultAsync(m => m.TeacherId == id);

            if (teacher == null) return NotFound();

            return View(teacher);
        }

        // GET: Teachers/Create
        public IActionResult Create()
        {
            ViewData["LevelId"] = new SelectList(_context.Level, "LevelId", "LevelName");
            return View();
        }

        // POST: Teachers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Teacher teacher, IFormFile? PictureFile)
        {
            // Remove validation errors for navigation properties
            ModelState.Remove("Level");

            if (ModelState.IsValid)
            {
                if (PictureFile != null && PictureFile.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(PictureFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("PictureFile", "Only image files are allowed (jpg, jpeg, png, gif)");
                        ViewData["LevelId"] = new SelectList(_context.Level, "LevelId", "LevelName", teacher.LevelId);
                        return View(teacher);
                    }

                    // Validate file size (max 5MB)
                    if (PictureFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("PictureFile", "File size must not exceed 5MB");
                        ViewData["LevelId"] = new SelectList(_context.Level, "LevelId", "LevelName", teacher.LevelId);
                        return View(teacher);
                    }

                    // Ensure folder exists
                    string uploadFolder = Path.Combine(_env.WebRootPath, "images/teachers");
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    // Generate unique file name
                    string fileName = Guid.NewGuid() + extension;
                    string filePath = Path.Combine(uploadFolder, fileName);

                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await PictureFile.CopyToAsync(stream);
                    }

                    teacher.PicturePath = "/images/teachers/" + fileName;
                }

                _context.Add(teacher);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Teacher created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["LevelId"] = new SelectList(_context.Level, "LevelId", "LevelName", teacher.LevelId);
            return View(teacher);
        }

        // GET: Teachers/Edit/5
        public async Task<IActionResult> Edit(int? id)
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
            ViewData["LevelId"] = new SelectList(_context.Level, "LevelId", "LevelName", teacher.LevelId);
            return View(teacher);
        }

        // POST: Teachers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Teacher teacher, IFormFile? PictureFile)
        {
            if (id != teacher.TeacherId)
            {
                return NotFound();
            }

            // Remove validation errors for navigation properties
            ModelState.Remove("Level");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing teacher to preserve picture path if needed
                    var existingTeacher = await _context.Teacher.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.TeacherId == id);

                    if (existingTeacher == null)
                    {
                        return NotFound();
                    }

                    // Handle picture upload
                    if (PictureFile != null && PictureFile.Length > 0)
                    {
                        // Validate file type
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var extension = Path.GetExtension(PictureFile.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("PictureFile", "Only image files are allowed (jpg, jpeg, png, gif)");
                            ViewData["LevelId"] = new SelectList(_context.Level, "LevelId", "LevelName", teacher.LevelId);
                            return View(teacher);
                        }

                        // Validate file size (max 5MB)
                        if (PictureFile.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("PictureFile", "File size must not exceed 5MB");
                            ViewData["LevelId"] = new SelectList(_context.Level, "LevelId", "LevelName", teacher.LevelId);
                            return View(teacher);
                        }

                        string uploadFolder = Path.Combine(_env.WebRootPath, "images/teachers");
                        if (!Directory.Exists(uploadFolder))
                            Directory.CreateDirectory(uploadFolder);

                        // Delete old picture if exists
                        if (!string.IsNullOrEmpty(existingTeacher.PicturePath))
                        {
                            string oldFilePath = Path.Combine(_env.WebRootPath, existingTeacher.PicturePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        string fileName = Guid.NewGuid() + extension;
                        string filePath = Path.Combine(uploadFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await PictureFile.CopyToAsync(stream);
                        }

                        teacher.PicturePath = "/images/teachers/" + fileName;
                    }
                    else
                    {
                        // Keep existing picture if no new one uploaded
                        teacher.PicturePath = existingTeacher.PicturePath;
                    }

                    _context.Update(teacher);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Teacher updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeacherExists(teacher.TeacherId))
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

            ViewData["LevelId"] = new SelectList(_context.Level, "LevelId", "LevelName", teacher.LevelId);
            return View(teacher);
        }

        // GET: Teachers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teacher
                .Include(t => t.Level)
                .FirstOrDefaultAsync(m => m.TeacherId == id);
            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        // POST: Teachers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacher = await _context.Teacher.FindAsync(id);
            if (teacher != null)
            {
                // Delete picture file if exists
                if (!string.IsNullOrEmpty(teacher.PicturePath))
                {
                    string filePath = Path.Combine(_env.WebRootPath, teacher.PicturePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Teacher.Remove(teacher);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Teacher deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TeacherExists(int id)
        {
            return _context.Teacher.Any(e => e.TeacherId == id);
        }
    }
}