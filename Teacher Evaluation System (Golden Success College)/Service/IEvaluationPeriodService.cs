using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;

namespace Teacher_Evaluation_System__Golden_Success_College_.Services
{
    public interface IEvaluationPeriodService
    {
        Task<EvaluationPeriod?> GetCurrentPeriodAsync();
        Task<bool> CanEvaluateAsync();
        Task<List<EvaluationPeriod>> GetAllPeriodsAsync();
        Task<bool> SetCurrentPeriodAsync(int periodId);
    }

    public class EvaluationPeriodService : IEvaluationPeriodService
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public EvaluationPeriodService(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        public async Task<EvaluationPeriod?> GetCurrentPeriodAsync()
        {
            return await _context.EvaluationPeriod
                .FirstOrDefaultAsync(p => p.IsCurrent && p.IsActive);
        }

        public async Task<bool> CanEvaluateAsync()
        {
            var currentPeriod = await GetCurrentPeriodAsync();
            return currentPeriod?.IsValidForEvaluation() ?? false;
        }

        public async Task<List<EvaluationPeriod>> GetAllPeriodsAsync()
        {
            return await _context.EvaluationPeriod
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();
        }

        public async Task<bool> SetCurrentPeriodAsync(int periodId)
        {
            // Remove current flag from all periods
            var allPeriods = await _context.EvaluationPeriod.ToListAsync();
            foreach (var period in allPeriods)
            {
                period.IsCurrent = false;
            }

            // Set new current period
            var newCurrentPeriod = await _context.EvaluationPeriod.FindAsync(periodId);
            if (newCurrentPeriod != null)
            {
                newCurrentPeriod.IsCurrent = true;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
}