using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class LevelsApiController : ControllerBase
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public LevelsApiController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: api/LevelsApi
        [HttpGet]
        public async Task<IActionResult> GetLevels()
        {
            var levels = await _context.Level.ToListAsync();

            return Ok(new
            {
                success = true,
                message = "Levels loaded successfully",
                data = levels
            });
        }

        // GET: api/LevelsApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLevel(int id)
        {
            var level = await _context.Level.FindAsync(id);

            if (level == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Level not found",
                    data = (Level?)null
                });
            }

            return Ok(new
            {
                success = true,
                message = "Level loaded successfully",
                data = level
            });
        }

        // POST: api/LevelsApi
        [HttpPost]
        public async Task<IActionResult> PostLevel([FromBody] Level level)
        {
            _context.Level.Add(level);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Level created successfully",
                data = level
            });
        }

        // PUT: api/LevelsApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLevel(int id, [FromBody] Level level)
        {
            if (id != level.LevelId)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Level ID mismatch"
                });
            }

            _context.Entry(level).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Level updated successfully",
                data = level
            });
        }

        // DELETE: api/LevelsApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLevel(int id)
        {
            var level = await _context.Level.FindAsync(id);
            if (level == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Level not found"
                });
            }

            _context.Level.Remove(level);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Level deleted successfully"
            });
        }
    }
}
