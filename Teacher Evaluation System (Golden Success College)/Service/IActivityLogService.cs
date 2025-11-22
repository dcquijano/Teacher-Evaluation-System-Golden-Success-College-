using System.Security.Claims;
using Teacher_Evaluation_System__Golden_Success_College_.Models;

namespace Teacher_Evaluation_System__Golden_Success_College_.Services
{
    public interface IActivityLogService
    {
        Task LogLoginAsync(ClaimsPrincipal user, string ipAddress, string userAgent);
        Task LogLogoutAsync(ClaimsPrincipal user, string ipAddress);
        Task LogEvaluationStartedAsync(int studentId, int teacherId, int subjectId, string ipAddress);
        Task LogEvaluationCompletedAsync(int studentId, int evaluationId, int teacherId, int subjectId, string ipAddress);
        Task<List<ActivityLog>> GetUserActivityLogsAsync(int? studentId, int? userId);
        Task<List<ActivityLog>> GetAllActivityLogsAsync(DateTime? fromDate, DateTime? toDate, string? activityType);
    }
}