using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;
using Teacher_Evaluation_System__Golden_Success_College_.Services;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    [Authorize(Roles = "Admin,Super Admin")]
    public class EvaluationPeriodsController : Controller
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;
        private readonly IEvaluationPeriodService _periodService;

        public EvaluationPeriodsController(
            Teacher_Evaluation_System__Golden_Success_College_Context context,
            IEvaluationPeriodService periodService)
        {
            _context = context;
            _periodService = periodService;
        }

        // GET: EvaluationPeriods
        public async Task<IActionResult> Index()
        {
            var periods = await _context.EvaluationPeriod
                .Include(p => p.Evaluations)
                .OrderByDescending(p => p.IsCurrent)
                .ThenByDescending(p => p.StartDate)
                .ToListAsync();

            return View(periods);
        }

        // GET: EvaluationPeriods/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var period = await _context.EvaluationPeriod
                .Include(p => p.Evaluations)
                    .ThenInclude(e => e.Teacher)
                .Include(p => p.Evaluations)
                    .ThenInclude(e => e.Student)
                .Include(p => p.Evaluations)
                    .ThenInclude(e => e.Subject)
                .FirstOrDefaultAsync(p => p.EvaluationPeriodId == id);

            if (period == null)
                return NotFound();

            return View(period);
        }

        // GET: EvaluationPeriods/Create
        public IActionResult Create()
        {
            var model = new EvaluationPeriod
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(3),
                IsActive = true,
                IsCurrent = false
            };
            return View(model);
        }

        // POST: EvaluationPeriods/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EvaluationPeriod model)
        {
            if (ModelState.IsValid)
            {
                // Validation: End date must be after start date
                if (model.EndDate <= model.StartDate)
                {
                    ModelState.AddModelError("EndDate", "End date must be after start date.");
                    return View(model);
                }

                // Check for overlapping periods
                var hasOverlap = await _context.EvaluationPeriod
                    .AnyAsync(p => p.IsActive &&
                        ((model.StartDate >= p.StartDate && model.StartDate <= p.EndDate) ||
                         (model.EndDate >= p.StartDate && model.EndDate <= p.EndDate) ||
                         (model.StartDate <= p.StartDate && model.EndDate >= p.EndDate)));

                if (hasOverlap)
                {
                    ModelState.AddModelError("", "This period overlaps with an existing active period.");
                    return View(model);
                }

                // If setting as current, remove current flag from other periods
                if (model.IsCurrent)
                {
                    var currentPeriods = await _context.EvaluationPeriod
                        .Where(p => p.IsCurrent)
                        .ToListAsync();

                    foreach (var period in currentPeriods)
                    {
                        period.IsCurrent = false;
                    }
                }

                model.CreatedDate = DateTime.Now;
                model.CreatedBy = User.Identity?.Name ?? "System";

                _context.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Evaluation period created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: EvaluationPeriods/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var period = await _context.EvaluationPeriod.FindAsync(id);
            if (period == null)
                return NotFound();

            return View(period);
        }

        // POST: EvaluationPeriods/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EvaluationPeriod model)
        {
            if (id != model.EvaluationPeriodId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Validation: End date must be after start date
                    if (model.EndDate <= model.StartDate)
                    {
                        ModelState.AddModelError("EndDate", "End date must be after start date.");
                        return View(model);
                    }

                    // Check for overlapping periods (excluding current period)
                    var hasOverlap = await _context.EvaluationPeriod
                        .AnyAsync(p => p.EvaluationPeriodId != id && p.IsActive &&
                            ((model.StartDate >= p.StartDate && model.StartDate <= p.EndDate) ||
                             (model.EndDate >= p.StartDate && model.EndDate <= p.EndDate) ||
                             (model.StartDate <= p.StartDate && model.EndDate >= p.EndDate)));

                    if (hasOverlap)
                    {
                        ModelState.AddModelError("", "This period overlaps with an existing active period.");
                        return View(model);
                    }

                    // If setting as current, remove current flag from other periods
                    if (model.IsCurrent)
                    {
                        var currentPeriods = await _context.EvaluationPeriod
                            .Where(p => p.IsCurrent && p.EvaluationPeriodId != id)
                            .ToListAsync();

                        foreach (var period in currentPeriods)
                        {
                            period.IsCurrent = false;
                        }
                    }

                    _context.Update(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Evaluation period updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await EvaluationPeriodExists(model.EvaluationPeriodId))
                        return NotFound();
                    else
                        throw;
                }
            }

            return View(model);
        }

        // POST: EvaluationPeriods/SetCurrent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCurrent(int id)
        {
            try
            {
                var success = await _periodService.SetCurrentPeriodAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Current evaluation period set successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to set current period. Period not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error setting current period: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: EvaluationPeriods/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var period = await _context.EvaluationPeriod.FindAsync(id);
                if (period != null)
                {
                    period.IsActive = !period.IsActive;

                    // If deactivating, also remove current status
                    if (!period.IsActive)
                    {
                        period.IsCurrent = false;
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Period {(period.IsActive ? "activated" : "deactivated")} successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Period not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error toggling period status: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: EvaluationPeriods/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var period = await _context.EvaluationPeriod
                .Include(p => p.Evaluations)
                .FirstOrDefaultAsync(p => p.EvaluationPeriodId == id);

            if (period == null)
                return NotFound();

            return View(period);
        }

        // POST: EvaluationPeriods/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var period = await _context.EvaluationPeriod
                    .Include(p => p.Evaluations)
                    .FirstOrDefaultAsync(p => p.EvaluationPeriodId == id);

                if (period != null)
                {
                    // Check if period has evaluations
                    if (period.Evaluations != null && period.Evaluations.Any())
                    {
                        TempData["ErrorMessage"] = $"Cannot delete period with existing evaluations. This period has {period.Evaluations.Count} evaluation(s).";
                        return RedirectToAction(nameof(Index));
                    }

                    _context.EvaluationPeriod.Remove(period);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Evaluation period deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Period not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting period: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: EvaluationPeriods/GetCurrent (API endpoint)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCurrent()
        {
            var currentPeriod = await _periodService.GetCurrentPeriodAsync();

            if (currentPeriod == null)
            {
                return Json(new
                {
                    success = false,
                    message = "No current evaluation period is set."
                });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    currentPeriod.EvaluationPeriodId,
                    currentPeriod.PeriodName,
                    currentPeriod.AcademicYear,
                    currentPeriod.Semester,
                    currentPeriod.StartDate,
                    currentPeriod.EndDate,
                    currentPeriod.IsActive,
                    Status = currentPeriod.Status,
                    IsValidForEvaluation = currentPeriod.IsValidForEvaluation()
                }
            });
        }

        // GET: EvaluationPeriods/CheckCanEvaluate (API endpoint)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> CheckCanEvaluate()
        {
            var canEvaluate = await _periodService.CanEvaluateAsync();
            var currentPeriod = await _periodService.GetCurrentPeriodAsync();

            return Json(new
            {
                success = true,
                canEvaluate = canEvaluate,
                currentPeriod = currentPeriod != null ? new
                {
                    currentPeriod.PeriodName,
                    currentPeriod.StartDate,
                    currentPeriod.EndDate,
                    Status = currentPeriod.Status
                } : null
            });
        }

        // GET: EvaluationPeriods/Statistics/5
        public async Task<IActionResult> Statistics(int? id)
        {
            if (id == null)
                return NotFound();

            var period = await _context.EvaluationPeriod
                .Include(p => p.Evaluations)
                    .ThenInclude(e => e.Teacher)
                .Include(p => p.Evaluations)
                    .ThenInclude(e => e.Student)
                .Include(p => p.Evaluations)
                    .ThenInclude(e => e.Subject)
                .Include(p => p.Evaluations)
                    .ThenInclude(e => e.Scores)
                .FirstOrDefaultAsync(p => p.EvaluationPeriodId == id);

            if (period == null)
                return NotFound();

            // Calculate statistics
            var stats = new
            {
                Period = period,
                TotalEvaluations = period.Evaluations?.Count ?? 0,
                UniqueStudents = period.Evaluations?.Select(e => e.StudentId).Distinct().Count() ?? 0,
                UniqueTeachers = period.Evaluations?.Select(e => e.TeacherId).Distinct().Count() ?? 0,
                AverageScore = period.Evaluations?.Any() == true ? period.Evaluations.Average(e => e.AverageScore) : 0,
                AnonymousCount = period.Evaluations?.Count(e => e.IsAnonymous) ?? 0,
                TopRatedTeachers = period.Evaluations?
                    .GroupBy(e => new { e.TeacherId, e.Teacher.FullName })
                    .Select(g => new
                    {
                        TeacherName = g.Key.FullName,
                        AverageScore = g.Average(e => e.AverageScore),
                        EvaluationCount = g.Count()
                    })
                    .OrderByDescending(t => t.AverageScore)
                    .Take(10)
                    .ToList()
            };

            return View(stats);
        }

        // Helper method
        private async Task<bool> EvaluationPeriodExists(int id)
        {
            return await _context.EvaluationPeriod.AnyAsync(e => e.EvaluationPeriodId == id);
        }
    }
}