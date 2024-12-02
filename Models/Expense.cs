namespace finguard_server.Models
{
    public class Expense
{
    public int Id { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public DateTime Date { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;
}

}
