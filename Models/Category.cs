using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectWeb.Models
{
public class Category
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    // This stores the Bootstrap Icon class (e.g., "bi-tag" or "bi-wallet")
    public string Icon { get; set; } = "bi-tag"; 

    // Helps distinguish between your hardcoded defaults and user-added ones
    public bool IsDefault { get; set; } = false;
    public string UserID { get; set; }

    // Navigation property (Relationship)
    public List<Expense> Expenses { get; set; }
}
}