using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;
using Teacher_Evaluation_System__Golden_Success_College_.ViewModels;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    public class QuestionsController : Controller
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public QuestionsController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: Questions
        public async Task<IActionResult> Index()
        {
            var questions = await _context.Question
        .Include(q => q.Criteria)
        .ToListAsync();

            var grouped = questions
                .GroupBy(q => new { q.CriteriaId, q.Criteria.Name })
                .Select(g => new QuestionIndexViewModel
                {
                    CriteriaId = g.Key.CriteriaId,
                    CriteriaName = g.Key.Name,
                    Descriptions = g.Select(q => q.Description ?? "").ToList(),
                    QuestionIds = g.Select(q => q.QuestionId).ToList()
                })
                .ToList();


            return View(grouped);
        }
        // GET: Questions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Question
                .Include(q => q.Criteria)
                .FirstOrDefaultAsync(m => m.QuestionId == id);

            if (question == null)
            {
                return NotFound();
            }

            // Load ALL questions for this criteria
            var allQuestionsForCriteria = await _context.Question
                .Include(q => q.Criteria)
                .Where(q => q.CriteriaId == question.CriteriaId)
                .OrderBy(q => q.QuestionId)
                .ToListAsync();

            // Map to ViewModel
            var viewModel = new QuestionIndexViewModel
            {
                CriteriaId = question.CriteriaId,
                CriteriaName = question.Criteria.Name,
                Descriptions = allQuestionsForCriteria.Select(q => q.Description).ToList(),
                QuestionIds = allQuestionsForCriteria.Select(q => q.QuestionId).ToList()
            };

            return View(viewModel);
        }

        // GET: Questions/Create
        public IActionResult Create()
        {
            ViewData["CriteriaId"] = new SelectList(_context.Criteria, "CriteriaId", "Name");
            return View();
        }

        // POST: Questions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int CriteriaId, List<string> Description)
        {
            if (ModelState.IsValid)
            {
                foreach (var desc in Description)
                {
                    var question = new Question
                    {
                        CriteriaId = CriteriaId,
                        Description = desc
                    };
                    _context.Add(question);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CriteriaId"] = new SelectList(_context.Criteria, "CriteriaId", "Name", CriteriaId);
            return View();
        }


        // GET: Questions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Question.FindAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            // Load ALL questions for this criteria
            var allQuestionsForCriteria = await _context.Question
                .Where(q => q.CriteriaId == question.CriteriaId)
                .OrderBy(q => q.QuestionId)
                .ToListAsync();

            ViewData["CriteriaId"] = new SelectList(_context.Criteria, "CriteriaId", "Name", question.CriteriaId);

            // Pass all questions instead of just one
            return View(allQuestionsForCriteria);
        }

        // POST: Questions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int CriteriaId, List<string> Description, List<int> QuestionIds)
        {
            if (Description == null || Description.Count == 0)
            {
                ModelState.AddModelError("Description", "Please enter at least one description.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original question to find its criteria
                    var originalQuestion = await _context.Question.FindAsync(id);
                    if (originalQuestion == null)
                        return NotFound();

                    // Get all existing questions for this criteria
                    var existingQuestions = await _context.Question
                        .Where(q => q.CriteriaId == originalQuestion.CriteriaId)
                        .ToListAsync();

                    // Remove all existing questions
                    _context.Question.RemoveRange(existingQuestions);

                    // Add all new/updated descriptions
                    foreach (var desc in Description)
                    {
                        if (!string.IsNullOrWhiteSpace(desc))
                        {
                            var question = new Question
                            {
                                CriteriaId = CriteriaId,
                                Description = desc.Trim()
                            };
                            _context.Add(question);
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestionExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["CriteriaId"] = new SelectList(_context.Criteria, "CriteriaId", "Name", CriteriaId);
            return View();
        }


        // GET: Questions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Question
                .Include(q => q.Criteria)
                .FirstOrDefaultAsync(m => m.QuestionId == id);

            if (question == null)
            {
                return NotFound();
            }

            // Load ALL questions for this criteria
            var allQuestionsForCriteria = await _context.Question
                .Include(q => q.Criteria)
                .Where(q => q.CriteriaId == question.CriteriaId)
                .OrderBy(q => q.QuestionId)
                .ToListAsync();

            // Map to ViewModel
            var viewModel = new QuestionIndexViewModel
            {
                CriteriaId = question.CriteriaId,
                CriteriaName = question.Criteria.Name,
                Descriptions = allQuestionsForCriteria.Select(q => q.Description).ToList(),
                QuestionIds = allQuestionsForCriteria.Select(q => q.QuestionId).ToList()
            };

            return View(viewModel);
        }

        // POST: Questions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var question = await _context.Question.FindAsync(id);

            if (question != null)
            {
                // Find all questions with the same CriteriaId
                var allQuestionsForCriteria = await _context.Question
                    .Where(q => q.CriteriaId == question.CriteriaId)
                    .ToListAsync();

                // Remove ALL questions for this criteria
                _context.Question.RemoveRange(allQuestionsForCriteria);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        private bool QuestionExists(int id)
        {
            return _context.Question.Any(e => e.QuestionId == id);
        }
    }
}
