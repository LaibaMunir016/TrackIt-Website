using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectWeb.Models
{
public class Saving
{
    public int SavingId { get; set; }

    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    public decimal Amount { get; set; }

    public DateTime DateSaved { get; set; }

    public string? Description { get; set; }

    //public string? AccountType { get; set; }
}
}
