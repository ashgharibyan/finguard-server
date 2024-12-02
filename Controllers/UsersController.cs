using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using finguard_server.Data;
using finguard_server.Models;
using FinguardServer.Dtos;

namespace finguard_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserReadDto>>> GetUsers()
        {
            var users = await _context.Users.Include(u => u.Expenses).ToListAsync();

            var userDtos = users.Select(user => new UserReadDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Expenses = user.Expenses.Select(e => new ExpenseDto
                {
                    Id = e.Id,
                    Description = e.Description,
                    Amount = e.Amount,
                    Date = e.Date
                }).ToList()
            });

            return Ok(userDtos);
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserReadDto>> GetUserById(int id)
        {
            var user = await _context.Users.Include(u => u.Expenses).FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound("User not found.");

            var userDto = new UserReadDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Expenses = user.Expenses.Select(e => new ExpenseDto
                {
                    Id = e.Id,
                    Description = e.Description,
                    Amount = e.Amount,
                    Date = e.Date
                }).ToList()
            };

            return Ok(userDto);
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<UserReadDto>> CreateUser(UserCreateDto userDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
                return Conflict("A user with this email already exists.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

            var user = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                PasswordHash = passwordHash
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userReadDto = new UserReadDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Expenses = new List<ExpenseDto>()
            };

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, userReadDto);
        }

        // POST: api/Users/login
        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserLoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return Unauthorized("Invalid email or password.");

            return Ok("Login successful!");
        }
    }
}
