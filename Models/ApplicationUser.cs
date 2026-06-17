using Microsoft.AspNetCore.Identity;

namespace ProjectWeb.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string? Currency { get; set; }
         public bool IsInDebt { get; set; } = false;
         public int? DebtNotifiedMonth { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        //unused for now
        public ICollection<Income> Incomes { get; set; } = new List<Income>();
    }
}