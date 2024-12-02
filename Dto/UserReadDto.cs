namespace FinguardServer.Dtos
{
    public class UserReadDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<ExpenseDto> Expenses { get; set; } = new();
    }
}
