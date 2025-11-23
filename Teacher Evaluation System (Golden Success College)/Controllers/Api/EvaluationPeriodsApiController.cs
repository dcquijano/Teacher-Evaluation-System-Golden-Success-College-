using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;
using Teacher_Evaluation_System__Golden_Success_College_.Services;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Super Admin")]
    public class EvaluationPeriodsApiController : ControllerBase
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;
        private readonly IEvaluationPeriodService _periodService;

        public EvaluationPeriodsApiController(
            Teacher_Evaluation_System__Golden_Success_College_Context context,
            IEvaluationPeriodService periodService)
        {
            _context = context;
            _periodService = periodService;
        }

        // GET: api/EvaluationPeriodsApi
        [HttpGet]
        public async Task<IActionResult> GetPeriods()
        {
            try
            {
                var periods = await _context.EvaluationPeriod
                    .Include(p => p.Evaluations)
                    .OrderByDescending(p => p.IsCurrent)
                    .ThenByDescending(p => p.StartDate)
                    .Select(p => new
                    {
                        p.EvaluationPeriodId,
                        p.PeriodName,
                        p.AcademicYear,
                        p.Semester,
                        p.StartDate,
                        p.EndDate,
                        p.IsActive,
                        p.IsCurrent,
                        p.Description,
                        Status = p.Status,
                        EvaluationCount = p.Evaluations != null ? p.Evaluations.Count : 0
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = periods,
                    message = "Periods retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving periods: {ex.Message}"
                });
            }
        }

        // GET: api/EvaluationPeriodsApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPeriod(int id)
        {
            try
            {
                var period = await _context.EvaluationPeriod
                    .Include(p => p.Evaluations)
                    .FirstOrDefaultAsync(p => p.EvaluationPeriodId == id);

                if (period == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Period not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        period.EvaluationPeriodId,
                        period.PeriodName,
                        period.AcademicYear,
                        period.Semester,
                        period.StartDate,
                        period.EndDate,
                        period.IsActive,
                        period.IsCurrent,
                        period.Description,
                        Status = period.Status,
                        EvaluationCount = period.Evaluations?.Count ?? 0
                    },
                    message = "Period retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving period: {ex.Message}"
                });
            }
        }

        // POST: api/EvaluationPeriodsApi
        [HttpPost]
        public async Task<IActionResult> CreatePeriod([FromBody] CreatePeriodDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid data",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                // Validation
                if (model.EndDate <= model.StartDate)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "End date must be after start date"
                    });
                }

                // Check for overlapping periods
                var hasOverlap = await _context.EvaluationPeriod
                    .AnyAsync(p => p.IsActive &&
                        ((model.StartDate >= p.StartDate && model.StartDate <= p.EndDate) ||
                         (model.EndDate >= p.StartDate && model.EndDate <= p.EndDate) ||
                         (model.StartDate <= p.StartDate && model.EndDate >= p.EndDate)));

                if (hasOverlap)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "This period overlaps with an existing active period"
                    });
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

                var newPeriod = new EvaluationPeriod
                {
                    PeriodName = model.PeriodName,
                    AcademicYear = model.AcademicYear,
                    Semester = model.Semester,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    IsActive = model.IsActive,
                    IsCurrent = model.IsCurrent,
                    Description = model.Description,
                    CreatedDate = DateTime.Now,
                    CreatedBy = User.Identity?.Name ?? "System"
                };

                _context.EvaluationPeriod.Add(newPeriod);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        newPeriod.EvaluationPeriodId,
                        newPeriod.PeriodName,
                        newPeriod.AcademicYear,
                        newPeriod.Semester,
                        newPeriod.StartDate,
                        newPeriod.EndDate,
                        newPeriod.IsActive,
                        newPeriod.IsCurrent,
                        Status = newPeriod.Status
                    },
                    message = "Period created successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error creating period: {ex.Message}"
                });
            }
        }

        // PUT: api/EvaluationPeriodsApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePeriod(int id, [FromBody] UpdatePeriodDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid data",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var period = await _context.EvaluationPeriod.FindAsync(id);
                if (period == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Period not found"
                    });
                }

                // Validation
                if (model.EndDate <= model.StartDate)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "End date must be after start date"
                    });
                }

                // Check for overlapping periods
                var hasOverlap = await _context.EvaluationPeriod
                    .AnyAsync(p => p.EvaluationPeriodId != id && p.IsActive &&
                        ((model.StartDate >= p.StartDate && model.StartDate <= p.EndDate) ||
                         (model.EndDate >= p.StartDate && model.EndDate <= p.EndDate) ||
                         (model.StartDate <= p.StartDate && model.EndDate >= p.EndDate)));

                if (hasOverlap)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "This period overlaps with an existing active period"
                    });
                }

                // If setting as current, remove current flag from other periods
                if (model.IsCurrent && !period.IsCurrent)
                {
                    var currentPeriods = await _context.EvaluationPeriod
                        .Where(p => p.IsCurrent && p.EvaluationPeriodId != id)
                        .ToListAsync();

                    foreach (var p in currentPeriods)
                    {
                        p.IsCurrent = false;
                    }
                }

                // Update period
                period.PeriodName = model.PeriodName;
                period.AcademicYear = model.AcademicYear;
                period.Semester = model.Semester;
                period.StartDate = model.StartDate;
                period.EndDate = model.EndDate;
                period.IsActive = model.IsActive;
                period.IsCurrent = model.IsCurrent;
                period.Description = model.Description;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        period.EvaluationPeriodId,
                        period.PeriodName,
                        period.AcademicYear,
                        period.Semester,
                        period.StartDate,
                        period.EndDate,
                        period.IsActive,
                        period.IsCurrent,
                        Status = period.Status
                    },
                    message = "Period updated successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error updating period: {ex.Message}"
                });
            }
        }

        // POST: api/EvaluationPeriodsApi/5/SetCurrent
        [HttpPost("{id}/SetCurrent")]
        public async Task<IActionResult> SetCurrent(int id)
        {
            try
            {
                var success = await _periodService.SetCurrentPeriodAsync(id);

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Current period set successfully"
                    });
                }

                return NotFound(new
                {
                    success = false,
                    message = "Period not found"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error setting current period: {ex.Message}"
                });
            }
        }

        // POST: api/EvaluationPeriodsApi/5/ToggleActive
        [HttpPost("{id}/ToggleActive")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var period = await _context.EvaluationPeriod.FindAsync(id);
                if (period == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Period not found"
                    });
                }

                period.IsActive = !period.IsActive;

                // If deactivating, also remove current status
                if (!period.IsActive)
                {
                    period.IsCurrent = false;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        period.EvaluationPeriodId,
                        period.IsActive,
                        period.IsCurrent
                    },
                    message = $"Period {(period.IsActive ? "activated" : "deactivated")} successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error toggling period: {ex.Message}"
                });
            }
        }

        // DELETE: api/EvaluationPeriodsApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePeriod(int id)
        {
            try
            {
                var period = await _context.EvaluationPeriod
                    .Include(p => p.Evaluations)
                    .FirstOrDefaultAsync(p => p.EvaluationPeriodId == id);

                if (period == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Period not found"
                    });
                }

                // Check if period has evaluations
                if (period.Evaluations != null && period.Evaluations.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Cannot delete period with existing evaluations. This period has {period.Evaluations.Count} evaluation(s)"
                    });
                }

                _context.EvaluationPeriod.Remove(period);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Period deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error deleting period: {ex.Message}"
                });
            }
        }

        // GET: api/EvaluationPeriodsApi/Current
        [HttpGet("Current")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCurrent()
        {
            var currentPeriod = await _periodService.GetCurrentPeriodAsync();

            if (currentPeriod == null)
            {
                return Ok(new
                {
                    success = false,
                    message = "No current evaluation period is set"
                });
            }

            return Ok(new
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
                },
                message = "Current period retrieved successfully"
            });
        }
    }

    // DTOs
    public class CreatePeriodDto
    {
        public string PeriodName { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsCurrent { get; set; } = false;
        public string? Description { get; set; }
    }

    public class UpdatePeriodDto
    {
        public string PeriodName { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsCurrent { get; set; }
        public string? Description { get; set; }
    }
}