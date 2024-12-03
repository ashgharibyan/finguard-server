using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using finguard_server.Data;
using finguard_server.Models;
using FinguardServer.Dtos;
using Microsoft.AspNetCore.Authorization;
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
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Invalid token");
                }

                var expenses = await _context.Expenses
                    .Where(e => e.UserId == userId)
                    .Select(e => new ExpenseDto
                    {
                        Id = e.Id,
                        Description = e.Description,
                        Amount = e.Amount,
                        Date = e.Date
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
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Invalid token");
                }

                var expense = await _context.Expenses
                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

                if (expense == null)
                {
                    return NotFound("Expense not found");
                }

                return Ok(new ExpenseDto
                {
                    Id = expense.Id,
                    Description = expense.Description,
                    Amount = expense.Amount,
                    Date = expense.Date
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
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Invalid token");
                }

                var expense = new Expense
                {
                    Description = expenseDto.Description,
                    Amount = expenseDto.Amount,
                    Date = expenseDto.Date,
                    UserId = userId
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
                        Date = expense.Date
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
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Invalid token");
                }

                var expense = await _context.Expenses
                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

                if (expense == null)
                {
                    return NotFound("Expense not found");
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
                    Date = expense.Date
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
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Invalid token");
                }

                var expense = await _context.Expenses
                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

                if (expense == null)
                {
                    return NotFound("Expense not found");
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
