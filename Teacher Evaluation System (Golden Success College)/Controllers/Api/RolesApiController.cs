using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesApiController : ControllerBase
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public RolesApiController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: api/RolesApi
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<Role>>>> GetRoles()
        {
            var roles = await _context.Role.ToListAsync();

            return new ApiResponse<IEnumerable<Role>>
            {
                Success = true,
                Message = "Roles loaded successfully",
                Data = roles
            };
        }

        // GET: api/RolesApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Role>>> GetRole(int id)
        {
            var role = await _context.Role.FindAsync(id);

            if (role == null)
            {
                return NotFound(new ApiResponse<Role>
                {
                    Success = false,
                    Message = "Role not found",
                    Data = null
                });
            }

            return new ApiResponse<Role>
            {
                Success = true,
                Message = "Role loaded successfully",
                Data = role
            };
        }

        // POST: api/RolesApi
        [HttpPost]
        public async Task<ActionResult<ApiResponse<Role>>> PostRole(Role role)
        {
            _context.Role.Add(role);
            await _context.SaveChangesAsync();

            return new ApiResponse<Role>
            {
                Success = true,
                Message = "Role created successfully",
                Data = role
            };
        }

        // PUT: api/RolesApi/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<Role>>> PutRole(int id, Role role)
        {
            if (id != role.RoleId)
            {
                return BadRequest(new ApiResponse<Role>
                {
                    Success = false,
                    Message = "Role ID mismatch",
                    Data = null
                });
            }

            _context.Entry(role).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return new ApiResponse<Role>
            {
                Success = true,
                Message = "Role updated successfully",
                Data = role
            };
        }

        // DELETE: api/RolesApi/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteRole(int id)
        {
            var role = await _context.Role.FindAsync(id);
            if (role == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Role not found",
                    Data = null
                });
            }

            _context.Role.Remove(role);
            await _context.SaveChangesAsync();

            return new ApiResponse<string>
            {
                Success = true,
                Message = "Role deleted successfully",
                Data = "Deleted"
            };
        }
    }
}
