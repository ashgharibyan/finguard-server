using System.ComponentModel.DataAnnotations;
namespace FinguardServer.Dtos
{
    public class ExpenseCreateDto
    {
        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int UserId { get; set; }
    }
}
