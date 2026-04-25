using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Models;

[Table("user_behavior_profiles")]
[Index(nameof(UserId), IsUnique = true)]
public class UserBehaviorProfile
{
    [Key]
    [Column("profile_id")]
    public Guid ProfileId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    public User User { get; set; } = default!;

    [Column("avg_transfer_amount", TypeName = "decimal(18,2)")]
    public decimal AvgTransferAmount { get; set; } = 0m;

    [Column("avg_daily_spend", TypeName = "decimal(18,2)")]
    public decimal AvgDailySpend { get; set; } = 0m;

    [Column("monthly_budget", TypeName = "decimal(18,2)")]
    public decimal MonthlyBudget { get; set; } = 0m;

    [Column("transfer_count")]
    public int TransferCount { get; set; } = 0;

    [Column("spend_count")]
    public int SpendCount { get; set; } = 0;

    [Column("typical_transfer_hour")]
    public int? TypicalTransferHour { get; set; }

    [MaxLength(20)]
    [Column("risk_tolerance")]
    public string RiskTolerance { get; set; } = "low";

    [Column("risk_score_baseline")]
    public int RiskScoreBaseline { get; set; } = 0;

    [Column("common_transfer_to_json", TypeName = "jsonb")]
    public string CommonTransferToJson { get; set; } = "[]";

    [Column("common_receive_from_json", TypeName = "jsonb")]
    public string CommonReceiveFromJson { get; set; } = "[]";

    [Column("common_categories_json", TypeName = "jsonb")]
    public string CommonCategoriesJson { get; set; } = "[]";

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
