using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using finguard_server.Data;
using finguard_server.Models;

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
        public async Task<IActionResult> GetExpenses()
        {
            var expenses = await _context.Expenses.Include(e => e.User).ToListAsync();
            return Ok(expenses);
        }

        // POST: api/Expenses
        [HttpPost]
        public async Task<IActionResult> CreateExpense(Expense expense)
        {
            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetExpenses), new { id = expense.Id }, expense);
        }
    }
}
