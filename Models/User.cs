namespace finguard_server.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}
