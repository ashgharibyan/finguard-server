namespace FinguardServer.Dtos
{
    public class UserReadDto
    {
        public string Id { get; set; } = string.Empty;  // Changed from int to string
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<ExpenseDto> Expenses { get; set; } = new List<ExpenseDto>();
    }
}