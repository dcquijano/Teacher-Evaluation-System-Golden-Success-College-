using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;
using Teacher_Evaluation_System__Golden_Success_College_.ViewModels;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionsApiController : ControllerBase
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public QuestionsApiController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: api/QuestionsApi
        [HttpGet]
        public async Task<IActionResult> GetQuestions()
        {
            try
            {
                var questions = await _context.Question
                    .Include(q => q.Criteria)
                    .ToListAsync();

                var grouped = questions
                    .GroupBy(q => new { q.CriteriaId, q.Criteria.Name })
                    .Select(g => new
                    {
                        criteriaId = g.Key.CriteriaId,
                        criteriaName = g.Key.Name,
                        descriptions = g.Select(q => q.Description).ToList(),
                        questionIds = g.Select(q => q.QuestionId).ToList()
                    })
                    .ToList();

                return Ok(new { success = true, data = grouped });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve questions: " + ex.Message });
            }
        }

        // GET: api/QuestionsApi/ByQuestion/5
        [HttpGet("ByQuestion/{questionId}")]
        public async Task<IActionResult> GetQuestionsByQuestion(int questionId)
        {
            var question = await _context.Question
                .Include(q => q.Criteria)
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            if (question == null)
                return NotFound(new { success = false, message = "Question not found" });

            var questionsForCriteria = await _context.Question
                .Where(q => q.CriteriaId == question.CriteriaId)
                .OrderBy(q => q.QuestionId)
                .ToListAsync();

            var data = new
            {
                criteriaId = question.CriteriaId,
                criteriaName = question.Criteria.Name,
                descriptions = questionsForCriteria.Select(q => q.Description).ToList(),
                questionIds = questionsForCriteria.Select(q => q.QuestionId).ToList()
            };

            return Ok(new { success = true, data });
        }



        // GET: api/QuestionsApi/ByCriteria/5
        [HttpGet("ByCriteria/{criteriaId}")]
        public async Task<IActionResult> GetQuestionsByCriteria(int criteriaId)
        {
            try
            {
                var criteria = await _context.Criteria.FindAsync(criteriaId);
                if (criteria == null)
                    return NotFound(new { success = false, message = "Criteria not found" });

                var questions = await _context.Question
                    .Where(q => q.CriteriaId == criteriaId)
                    .OrderBy(q => q.QuestionId)
                    .ToListAsync();

                var data = new
                {
                    criteriaId = criteriaId,
                    criteriaName = criteria.Name,
                    descriptions = questions.Select(q => q.Description).ToList(),
                    questionIds = questions.Select(q => q.QuestionId).ToList()
                };

                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve questions: " + ex.Message });
            }
        }

        // POST: api/QuestionsApi
        [HttpPost]
        public async Task<IActionResult> CreateQuestions([FromBody] CreateQuestionsRequest request)
        {
            try
            {
                if (request.CriteriaId == 0)
                    return BadRequest(new { success = false, message = "Criteria is required." });

                if (request.Descriptions == null || !request.Descriptions.Any())
                    return BadRequest(new { success = false, message = "At least one description is required." });

                // Verify criteria exists
                var criteriaExists = await _context.Criteria.AnyAsync(c => c.CriteriaId == request.CriteriaId);
                if (!criteriaExists)
                    return BadRequest(new { success = false, message = "Invalid Criteria selected." });

                var questionIds = new List<int>();

                foreach (var desc in request.Descriptions)
                {
                    if (!string.IsNullOrWhiteSpace(desc))
                    {
                        var question = new Question
                        {
                            CriteriaId = request.CriteriaId,
                            Description = desc.Trim()
                        };
                        _context.Question.Add(question);
                        await _context.SaveChangesAsync();
                        questionIds.Add(question.QuestionId);
                    }
                }

                var criteria = await _context.Criteria.FindAsync(request.CriteriaId);

                return Ok(new
                {
                    success = true,
                    message = "Questions created successfully",
                    data = new
                    {
                        criteriaId = request.CriteriaId,
                        criteriaName = criteria?.Name,
                        descriptions = request.Descriptions.Where(d => !string.IsNullOrWhiteSpace(d)).ToList(),
                        questionIds
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to save questions: " + ex.Message });
            }
        }

        // PUT: api/QuestionsApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuestions(int id, [FromBody] UpdateQuestionsRequest request)
        {
            try
            {
                // Get the original question to find its criteria
                var originalQuestion = await _context.Question.FindAsync(id);
                if (originalQuestion == null)
                    return NotFound(new { success = false, message = "Question not found" });

                if (request.CriteriaId == 0)
                    return BadRequest(new { success = false, message = "Criteria is required." });

                if (request.Descriptions == null || !request.Descriptions.Any())
                    return BadRequest(new { success = false, message = "At least one description is required." });

                // Verify criteria exists
                var criteriaExists = await _context.Criteria.AnyAsync(c => c.CriteriaId == request.CriteriaId);
                if (!criteriaExists)
                    return BadRequest(new { success = false, message = "Invalid Criteria selected." });

                // Get all existing questions for this criteria
                var existingQuestions = await _context.Question
                    .Where(q => q.CriteriaId == originalQuestion.CriteriaId)
                    .ToListAsync();

                // Remove all existing questions
                _context.Question.RemoveRange(existingQuestions);

                var questionIds = new List<int>();

                // Add all new/updated descriptions
                foreach (var desc in request.Descriptions)
                {
                    if (!string.IsNullOrWhiteSpace(desc))
                    {
                        var question = new Question
                        {
                            CriteriaId = request.CriteriaId,
                            Description = desc.Trim()
                        };
                        _context.Question.Add(question);
                        await _context.SaveChangesAsync();
                        questionIds.Add(question.QuestionId);
                    }
                }

                await _context.SaveChangesAsync();

                var criteria = await _context.Criteria.FindAsync(request.CriteriaId);

                return Ok(new
                {
                    success = true,
                    message = "Questions updated successfully",
                    data = new
                    {
                        criteriaId = request.CriteriaId,
                        criteriaName = criteria?.Name,
                        descriptions = request.Descriptions.Where(d => !string.IsNullOrWhiteSpace(d)).ToList(),
                        questionIds
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update questions: " + ex.Message });
            }
        }

        // DELETE: api/QuestionsApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestions(int id)
        {
            try
            {
                var question = await _context.Question.FindAsync(id);
                if (question == null)
                    return NotFound(new { success = false, message = "Question not found" });

                // Find all questions with the same CriteriaId
                var allQuestionsForCriteria = await _context.Question
                    .Where(q => q.CriteriaId == question.CriteriaId)
                    .ToListAsync();

                // Remove ALL questions for this criteria
                _context.Question.RemoveRange(allQuestionsForCriteria);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Questions deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to delete questions: " + ex.Message });
            }
        }
    }
}