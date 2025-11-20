using BCrypt.Net;
using Microsoft.CodeAnalysis.Scripting;

namespace Teacher_Evaluation_System__Golden_Success_College_.Helper
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        public static bool VerifyPassword(string enteredPassword, string storedHashedPassword)
        {
            if (string.IsNullOrEmpty(enteredPassword) || string.IsNullOrEmpty(storedHashedPassword))
            {
                // Either password or hash is null/empty, cannot verify
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(enteredPassword, storedHashedPassword);
            }
            catch (SaltParseException ex)
            {
                // Log error if needed
                Console.WriteLine($"Error verifying password: {ex.Message}");
                return false;
            }
        }
    }
}
