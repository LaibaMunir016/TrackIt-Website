using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectWeb.Models
{
public class Goal
{
    public int GoalId { get; set; }

    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    public string Title { get; set; }

    public decimal TargetAmount { get; set; }

    public decimal CurrentAmount { get; set; } = 0;

    public DateTime StartDate { get; set; }

    public DateTime TargetDate { get; set; }

    public bool IsAchieved { get; set; } = false;
    public DateTime? AchievedDate { get; set; }

    public string? Notes { get; set; }
}

}
