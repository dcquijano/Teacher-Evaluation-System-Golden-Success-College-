using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Helper;
using Teacher_Evaluation_System__Golden_Success_College_.Models;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersApiController : ControllerBase
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public UsersApiController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: api/UsersApi
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.User
                .Include(u => u.Role)
                .ToListAsync();

            var data = users.Select(u => new
            {
                userId = u.UserId,
                fullName = u.FullName,
                email = u.Email,
                roleId = u.RoleId,
                role = u.Role != null ? new { id = u.RoleId, name = u.Role.Name } : null
            });

            return Ok(new
            {
                success = true,
                data = data
            });
        }

        // GET: api/UsersApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.User
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
                return NotFound(new { success = false, message = "User not found" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    userId = user.UserId,
                    fullName = user.FullName,
                    email = user.Email,
                    roleId = user.RoleId,
                    role = user.Role != null ? new { id = user.RoleId, name = user.Role.Name } : null
                }
            });
        }

        // POST: api/UsersApi
        [HttpPost]
        public async Task<IActionResult> PostUser(User user)
        {
            // Hash password
            user.Password = PasswordHelper.HashPassword(user.Password);

            _context.User.Add(user);
            await _context.SaveChangesAsync();

            var role = await _context.Role.FirstOrDefaultAsync(r => r.RoleId == user.RoleId);

            return Ok(new
            {
                success = true,
                message = "User created successfully",
                data = new
                {
                    userId = user.UserId,
                    fullName = user.FullName,
                    email = user.Email,
                    roleId = user.RoleId,
                    role = role != null ? new { id = role.RoleId, name = role.Name } : null
                }
            });
        }

        // PUT: api/UsersApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.UserId)
                return BadRequest(new { success = false, message = "User ID mismatch" });

            var existingUser = await _context.User.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
            if (existingUser == null)
                return NotFound(new { success = false, message = "User not found" });

            // If password changed, re-hash
            if (!string.IsNullOrWhiteSpace(user.Password) &&
                !PasswordHelper.VerifyPassword(user.Password, existingUser.Password))
            {
                user.Password = PasswordHelper.HashPassword(user.Password);
            }
            else
            {
                user.Password = existingUser.Password;
            }

            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            var role = await _context.Role.FirstOrDefaultAsync(r => r.RoleId == user.RoleId);

            return Ok(new
            {
                success = true,
                message = "User updated successfully",
                data = new
                {
                    userId = user.UserId,
                    fullName = user.FullName,
                    email = user.Email,
                    roleId = user.RoleId,
                    role = role != null ? new { id = role.RoleId, name = role.Name } : null
                }
            });
        }

        // DELETE: api/UsersApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.User.FindAsync(id);
            if (user == null)
                return NotFound(new { success = false, message = "User not found" });

            _context.User.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "User deleted successfully"
            });
        }
    }
}
