using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using finguard_server.Data;
using finguard_server.Models;
using FinguardServer.Dtos;
using System.Security.Claims;

namespace finguard_server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ExpensesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ExpensesController> _logger;

        public ExpensesController(AppDbContext context, ILogger<ExpensesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetExpenses()
        {
            try
            {
                // Now we get all expenses, not just user-specific ones
                var expenses = await _context.Expenses
                    .Select(e => new ExpenseDto
                    {
                        Id = e.Id,
                        Description = e.Description,
                        Amount = e.Amount,
                        Date = e.Date,
                        CreatedBy = e.CreatedByEmail,
                        CreatedAt = e.CreatedAt
                    })
                    .ToListAsync();

                return Ok(expenses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving expenses");
                return StatusCode(500, "An error occurred while retrieving expenses");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseDto>> GetExpenseById(int id)
        {
            try
            {
                var expense = await _context.Expenses
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (expense == null)
                {
                    return NotFound("Expense not found");
                }

                return Ok(new ExpenseDto
                {
                    Id = expense.Id,
                    Description = expense.Description,
                    Amount = expense.Amount,
                    Date = expense.Date,
                    CreatedBy = expense.CreatedByEmail,
                    CreatedAt = expense.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving expense");
                return StatusCode(500, "An error occurred while retrieving the expense");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ExpenseDto>> CreateExpense(ExpenseCreateDto expenseDto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userEmail = User.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User information not found in token");
                }

                var expense = new Expense
                {
                    Description = expenseDto.Description,
                    Amount = expenseDto.Amount,
                    Date = expenseDto.Date,
                    CreatedById = userId,
                    CreatedByEmail = userEmail,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Expenses.Add(expense);
                await _context.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetExpenseById),
                    new { id = expense.Id },
                    new ExpenseDto
                    {
                        Id = expense.Id,
                        Description = expense.Description,
                        Amount = expense.Amount,
                        Date = expense.Date,
                        CreatedBy = expense.CreatedByEmail,
                        CreatedAt = expense.CreatedAt
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expense");
                return StatusCode(500, "An error occurred while creating the expense");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ExpenseDto>> UpdateExpense(int id, ExpenseCreateDto expenseDto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogInformation($"Update attempt - User ID: {userId}");

                var expense = await _context.Expenses.FindAsync(id);

                if (expense == null)
                {
                    _logger.LogWarning($"Expense not found - ID: {id}");
                    return NotFound("Expense not found");
                }

                _logger.LogInformation($"Expense creator ID: {expense.CreatedById}");
                _logger.LogInformation($"Current user ID: {userId}");

                // Check if the current user is the creator of the expense
                if (!string.Equals(expense.CreatedById, userId, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Unauthorized update attempt - User {userId} trying to update expense created by {expense.CreatedById}");
                    return Unauthorized("You can only update your own expenses");
                }

                expense.Description = expenseDto.Description;
                expense.Amount = expenseDto.Amount;
                expense.Date = expenseDto.Date;

                await _context.SaveChangesAsync();

                return Ok(new ExpenseDto
                {
                    Id = expense.Id,
                    Description = expense.Description,
                    Amount = expense.Amount,
                    Date = expense.Date,
                    CreatedBy = expense.CreatedByEmail,
                    CreatedAt = expense.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expense");
                return StatusCode(500, "An error occurred while updating the expense");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogInformation($"Delete attempt - User ID: {userId}");

                var expense = await _context.Expenses.FindAsync(id);

                if (expense == null)
                {
                    _logger.LogWarning($"Expense not found - ID: {id}");
                    return NotFound("Expense not found");
                }

                _logger.LogInformation($"Expense creator ID: {expense.CreatedById}");
                _logger.LogInformation($"Current user ID: {userId}");

                // Check if the current user is the creator of the expense
                if (!string.Equals(expense.CreatedById, userId, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Unauthorized delete attempt - User {userId} trying to delete expense created by {expense.CreatedById}");
                    return Unauthorized("You can only delete your own expenses");
                }

                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expense");
                return StatusCode(500, "An error occurred while deleting the expense");
            }
        }
    }
}