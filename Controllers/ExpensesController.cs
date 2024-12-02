using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using finguard_server.Data;
using finguard_server.Models;
using FinguardServer.Dtos;

namespace finguard_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExpensesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExpensesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Expenses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetExpenses()
        {
            var expenses = await _context.Expenses.Include(e => e.User).ToListAsync();

            var expenseDtos = expenses.Select(expense => new ExpenseDto
            {
                Id = expense.Id,
                Description = expense.Description,
                Amount = expense.Amount,
                Date = expense.Date
            });

            return Ok(expenseDtos);
        }

        // POST: api/Expenses
        [HttpPost]
        public async Task<ActionResult<ExpenseDto>> CreateExpense(ExpenseCreateDto expenseDto)
        {
            var user = await _context.Users.FindAsync(expenseDto.UserId);
            if (user == null)
                return NotFound("User not found.");

            var expense = new Expense
            {
                Description = expenseDto.Description,
                Amount = expenseDto.Amount,
                Date = expenseDto.Date,
                UserId = expenseDto.UserId
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            var createdExpenseDto = new ExpenseDto
            {
                Id = expense.Id,
                Description = expense.Description,
                Amount = expense.Amount,
                Date = expense.Date
            };

            return CreatedAtAction(nameof(GetExpenses), new { id = expense.Id }, createdExpenseDto);
        }
    }
}
