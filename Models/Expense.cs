using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectWeb.Models
{
public class Expense
{
    public int ExpenseId { get; set; }
    
    public string UserId { get; set; }//foreign key
    public ApplicationUser User { get; set; }

    public decimal Amount { get; set; }

    [Required]
    public int CategoryId { get; set; } // The Foreign Key ID

    [ForeignKey("CategoryId")]
    
    public Category Category { get; set; } // The Navigation Property  

    public DateTime DateSpent { get; set; }

    public string PaymentMethod { get; set; } 

    public string? Description { get; set; }  // optional
}
}
