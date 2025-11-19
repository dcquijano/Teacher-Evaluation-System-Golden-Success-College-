using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Helper;
using Teacher_Evaluation_System__Golden_Success_College_.Models;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserApiController : ControllerBase
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public UserApiController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: api/UserApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.User.Include(u => u.Role)
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

        // POST: api/UserApi
        [HttpPost]
        public async Task<IActionResult> PostUser(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.Password))
                user.Password = PasswordHelper.HashPassword(user.Password);

            _context.User.Add(user);
            await _context.SaveChangesAsync();

            // Load role
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
                    role = new { id = role.RoleId, name = role.Name }
                }
            });
        }

        // PUT: api/UserApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.UserId)
                return BadRequest(new { success = false, message = "User ID mismatch" });

            var existingUser = await _context.User.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
            if (existingUser == null)
                return NotFound(new { success = false, message = "User not found" });

            if (!string.IsNullOrWhiteSpace(user.Password))
            {
                if (!PasswordHelper.VerifyPassword(user.Password, existingUser.Password))
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
                    role = new { id = role.RoleId, name = role.Name }
                }
            });
        }

        // DELETE: api/UserApi/5
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
