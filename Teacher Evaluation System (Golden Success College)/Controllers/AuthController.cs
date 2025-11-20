using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Helper;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    public class AuthController : Controller
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public AuthController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: Login Page
        public IActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Email and Password are required.");
                return View();
            }

            // Try User Login
            var user = await _context.User
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user != null && !string.IsNullOrEmpty(user.Password) && PasswordHelper.VerifyPassword(password, user.Password))
            {
                await SignInUser(user.UserId, user.FullName!, user.Role!.Name);
                return RedirectToAction("Index", "Dashboard");
            }

            // Try Student Login
            var student = await _context.Student
                .Include(s => s.Role)
                .FirstOrDefaultAsync(s => s.Email == email);

            if (student != null && PasswordHelper.VerifyPassword(password, student.Password))
            {
                await SignInUser(student.StudentId, student.FullName!, student.Role!.Name);
                return RedirectToAction("Index", "Dashboard");
            }

            // Failed Login
            ModelState.AddModelError("", "Invalid Email or Password.");
            return View();
        }

        // Sign In Method (CREATES COOKIE)
        private async Task SignInUser(int id, string fullName, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, role),
                new Claim("RoleName", role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Create Cookie
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Optional: Session
            HttpContext.Session.SetInt32("UserId", id);
            HttpContext.Session.SetString("FullName", fullName);
            HttpContext.Session.SetString("Role", role);
        }

        // Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Access Denied Page
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
