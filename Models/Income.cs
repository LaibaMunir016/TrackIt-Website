using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectWeb.Models
{
    public class Income
    {
        // Primary Key
        [Key]
        public int IncomeID { get; set; }

        // Foreign Key: Must be 'string' to match the ApplicationUser ID type
        [Required]
        public string UserID { get; set; }

        // Date the income was received.
        [Required]
        [DataType(DataType.Date)]
        public DateTime DateReceived { get; set; }
        [Required]
        [Column(TypeName = "decimal(18, 2)")] 
        public decimal Amount { get; set; }

        [StringLength(100)]
        public string Source { get; set; } = string.Empty;

     
        [StringLength(255)]
        public string? Description { get; set; }

        public ApplicationUser? ApplicationUser { get; set; }
    }
}