using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class SectionsApiController : ControllerBase
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public SectionsApiController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: api/SectionsApi
        [HttpGet]
        public async Task<IActionResult> GetSections()
        {
            var sections = await _context.Section
                .Include(s => s.Level)
                .ToListAsync();

            var data = sections.Select(s => new
            {
                sectionId = s.SectionId,
                sectionName = s.SectionName,
                levelId = s.LevelId,
                level = s.Level != null ? new { id = s.LevelId, name = s.Level.LevelName } : null
            });

            return Ok(new
            {
                success = true,
                data = data
            });
        }

        // GET: api/SectionsApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSection(int id)
        {
            var section = await _context.Section.Include(s => s.Level)
                                                 .FirstOrDefaultAsync(s => s.SectionId == id);

            if (section == null)
                return NotFound(new { success = false, message = "Section not found" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    sectionId = section.SectionId,
                    sectionName = section.SectionName,
                    levelId = section.LevelId,
                    level = section.Level != null ? new { id = section.LevelId, name = section.Level.LevelName } : null
                }
            });
        }

        // POST: api/SectionsApi
        [HttpPost]
        public async Task<IActionResult> PostSection(Section section)
        {
            _context.Section.Add(section);
            await _context.SaveChangesAsync();

            // Load level
            var level = await _context.Level.FirstOrDefaultAsync(l => l.LevelId == section.LevelId);

            return Ok(new
            {
                success = true,
                message = "Section created successfully",
                data = new
                {
                    sectionId = section.SectionId,
                    sectionName = section.SectionName,
                    levelId = section.LevelId,
                    level = new { id = level.LevelId, name = level.LevelName }
                }
            });
        }

        // PUT: api/SectionsApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSection(int id, Section section)
        {
            if (id != section.SectionId)
                return BadRequest(new { success = false, message = "Section ID mismatch" });

            var existingSection = await _context.Section.AsNoTracking().FirstOrDefaultAsync(s => s.SectionId == id);
            if (existingSection == null)
                return NotFound(new { success = false, message = "Section not found" });

            _context.Entry(section).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            var level = await _context.Level.FirstOrDefaultAsync(l => l.LevelId == section.LevelId);

            return Ok(new
            {
                success = true,
                message = "Section updated successfully",
                data = new
                {
                    sectionId = section.SectionId,
                    sectionName = section.SectionName,
                    levelId = section.LevelId,
                    level = new { id = level.LevelId, name = level.LevelName }
                }
            });
        }

        // DELETE: api/SectionsApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSection(int id)
        {
            var section = await _context.Section.FindAsync(id);
            if (section == null)
                return NotFound(new { success = false, message = "Section not found" });

            _context.Section.Remove(section);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Section deleted successfully"
            });
        }
    }
}