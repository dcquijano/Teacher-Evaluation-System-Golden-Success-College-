using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeachersApiController : ControllerBase
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;
        private readonly IWebHostEnvironment _env;

        public TeachersApiController(Teacher_Evaluation_System__Golden_Success_College_Context context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: api/TeachersApi
        [HttpGet]
        public async Task<IActionResult> GetTeachers()
        {
            try
            {
                var teachers = await _context.Teacher
                    .Include(t => t.Level)
                    .ToListAsync();

                var data = teachers.Select(t => new
                {
                    teacherId = t.TeacherId,
                    fullName = t.FullName,
                    department = t.Department,
                    levelId = t.LevelId,
                    level = t.Level != null ? new { id = t.LevelId, name = t.Level.LevelName } : null,
                    picturePath = t.PicturePath,
                    isActive = t.IsActive
                });

                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve teachers: " + ex.Message });
            }
        }

        // GET: api/TeachersApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeacher(int id)
        {
            try
            {
                var teacher = await _context.Teacher
                    .Include(t => t.Level)
                    .FirstOrDefaultAsync(t => t.TeacherId == id);

                if (teacher == null)
                    return NotFound(new { success = false, message = "Teacher not found" });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        teacherId = teacher.TeacherId,
                        fullName = teacher.FullName,
                        department = teacher.Department,
                        levelId = teacher.LevelId,
                        level = teacher.Level != null ? new { id = teacher.LevelId, name = teacher.Level.LevelName } : null,
                        picturePath = teacher.PicturePath,
                        isActive = teacher.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve teacher: " + ex.Message });
            }
        }

        // POST: api/TeachersApi
        [HttpPost]
        public async Task<IActionResult> CreateTeacher([FromForm] string fullName, [FromForm] string department,
            [FromForm] int levelId, [FromForm] bool isActive, [FromForm] IFormFile? pictureFile)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(fullName))
                    return BadRequest(new { success = false, message = "Full Name is required." });

                if (string.IsNullOrWhiteSpace(department))
                    return BadRequest(new { success = false, message = "Department is required." });

                if (levelId == 0)
                    return BadRequest(new { success = false, message = "Level is required." });

                // Verify level exists
                var levelExists = await _context.Level.AnyAsync(l => l.LevelId == levelId);
                if (!levelExists)
                    return BadRequest(new { success = false, message = "Invalid Level selected." });

                var teacher = new Teacher
                {
                    FullName = fullName.Trim(),
                    Department = department.Trim(),
                    LevelId = levelId,
                    IsActive = isActive
                };

                // Handle picture upload
                if (pictureFile != null && pictureFile.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(pictureFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                        return BadRequest(new { success = false, message = "Only image files are allowed (jpg, jpeg, png, gif)." });

                    // Validate file size (max 5MB)
                    if (pictureFile.Length > 5 * 1024 * 1024)
                        return BadRequest(new { success = false, message = "File size must not exceed 5MB." });

                    string uploadFolder = Path.Combine(_env.WebRootPath, "images/teachers");
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    string fileName = Guid.NewGuid() + extension;
                    string filePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await pictureFile.CopyToAsync(stream);
                    }

                    teacher.PicturePath = "/images/teachers/" + fileName;
                }

                _context.Teacher.Add(teacher);
                await _context.SaveChangesAsync();

                // Reload with Level for response
                var savedTeacher = await _context.Teacher
                    .Include(t => t.Level)
                    .FirstOrDefaultAsync(t => t.TeacherId == teacher.TeacherId);

                return Ok(new
                {
                    success = true,
                    message = "Teacher created successfully",
                    data = new
                    {
                        teacherId = savedTeacher.TeacherId,
                        fullName = savedTeacher.FullName,
                        department = savedTeacher.Department,
                        levelId = savedTeacher.LevelId,
                        level = savedTeacher.Level != null ? new { id = savedTeacher.LevelId, name = savedTeacher.Level.LevelName } : null,
                        picturePath = savedTeacher.PicturePath,
                        isActive = savedTeacher.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to save teacher: " + ex.Message });
            }
        }

        // PUT: api/TeachersApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeacher(int id, [FromForm] string fullName, [FromForm] string department,
            [FromForm] int levelId, [FromForm] bool isActive, [FromForm] IFormFile? pictureFile)
        {
            try
            {
                var existingTeacher = await _context.Teacher.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TeacherId == id);

                if (existingTeacher == null)
                    return NotFound(new { success = false, message = "Teacher not found" });

                // Validate required fields
                if (string.IsNullOrWhiteSpace(fullName))
                    return BadRequest(new { success = false, message = "Full Name is required." });

                if (string.IsNullOrWhiteSpace(department))
                    return BadRequest(new { success = false, message = "Department is required." });

                if (levelId == 0)
                    return BadRequest(new { success = false, message = "Level is required." });

                // Verify level exists
                var levelExists = await _context.Level.AnyAsync(l => l.LevelId == levelId);
                if (!levelExists)
                    return BadRequest(new { success = false, message = "Invalid Level selected." });

                var teacher = new Teacher
                {
                    TeacherId = id,
                    FullName = fullName.Trim(),
                    Department = department.Trim(),
                    LevelId = levelId,
                    IsActive = isActive,
                    PicturePath = existingTeacher.PicturePath
                };

                // Handle picture upload
                if (pictureFile != null && pictureFile.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(pictureFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                        return BadRequest(new { success = false, message = "Only image files are allowed (jpg, jpeg, png, gif)." });

                    // Validate file size (max 5MB)
                    if (pictureFile.Length > 5 * 1024 * 1024)
                        return BadRequest(new { success = false, message = "File size must not exceed 5MB." });

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
                        await pictureFile.CopyToAsync(stream);
                    }

                    teacher.PicturePath = "/images/teachers/" + fileName;
                }

                _context.Entry(teacher).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // Reload with Level for response
                var updatedTeacher = await _context.Teacher
                    .Include(t => t.Level)
                    .FirstOrDefaultAsync(t => t.TeacherId == id);

                return Ok(new
                {
                    success = true,
                    message = "Teacher updated successfully",
                    data = new
                    {
                        teacherId = updatedTeacher.TeacherId,
                        fullName = updatedTeacher.FullName,
                        department = updatedTeacher.Department,
                        levelId = updatedTeacher.LevelId,
                        level = updatedTeacher.Level != null ? new { id = updatedTeacher.LevelId, name = updatedTeacher.Level.LevelName } : null,
                        picturePath = updatedTeacher.PicturePath,
                        isActive = updatedTeacher.IsActive
                    }
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound(new { success = false, message = "Teacher not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update teacher: " + ex.Message });
            }
        }

        // DELETE: api/TeachersApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            try
            {
                var teacher = await _context.Teacher.FindAsync(id);
                if (teacher == null)
                    return NotFound(new { success = false, message = "Teacher not found" });

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

                return Ok(new { success = true, message = "Teacher deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to delete teacher: " + ex.Message });
            }
        }
    }
}