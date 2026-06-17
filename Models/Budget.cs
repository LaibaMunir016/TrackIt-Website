using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectWeb.Models
{
public class Budget
{
    public int BudgetId { get; set; }

    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    public string Category { get; set; }

    public decimal Amount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? Notes { get; set; }

    public bool IsRecurring { get; set; } = false; // optional
}
}