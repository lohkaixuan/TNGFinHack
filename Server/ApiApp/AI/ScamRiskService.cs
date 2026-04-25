namespace ApiApp.AI;

public record ScamRiskInput(
    decimal Amount,
    string ReceiverName,
    bool IsNewReceiver,
    int TransferCountToday,
    string? Note,
    string? ReceiverKey = null,
    string? Category = null,
    int? TransferHour = null,
    UserBehaviorContext? Behavior = null
);

public record ScamRiskResult(
    int Score,
    string Level,
    List<string> Reasons
);

public class ScamRiskService
{
    public ScamRiskResult Analyze(ScamRiskInput input)
    {
        var score = 0;
        var reasons = new List<string>();

        if (input.IsNewReceiver)
        {
            score += 25;
            reasons.Add("New receiver");
        }

        if (input.Amount >= 1000)
        {
            score += 25;
            reasons.Add("Large transfer amount");
        }

        if (input.Behavior is not null)
        {
            var behavior = input.Behavior;

            if (behavior.AvgTransferAmount > 0 && input.Amount >= behavior.AvgTransferAmount * 8)
            {
                score += 35;
                reasons.Add($"Amount is unusually high: about {Math.Round(input.Amount / behavior.AvgTransferAmount, 1)}x your usual transfer");
            }
            else if (behavior.AvgTransferAmount > 0 && input.Amount >= behavior.AvgTransferAmount * 3)
            {
                score += 20;
                reasons.Add($"Amount is higher than usual: about {Math.Round(input.Amount / behavior.AvgTransferAmount, 1)}x your usual transfer");
            }

            if (!string.IsNullOrWhiteSpace(input.ReceiverKey) &&
                behavior.CommonTransferTo.Count > 0 &&
                !behavior.CommonTransferTo.Contains(input.ReceiverKey, StringComparer.OrdinalIgnoreCase))
            {
                score += 15;
                reasons.Add("Receiver is not in your usual receiver pattern");
            }

            if (!string.IsNullOrWhiteSpace(input.Category) &&
                behavior.CommonCategories.Count > 0 &&
                !behavior.CommonCategories.Contains(input.Category, StringComparer.OrdinalIgnoreCase))
            {
                score += 10;
                reasons.Add("Category is outside your usual spending pattern");
            }

            if (input.TransferHour is int hour && behavior.TypicalTransferHour is int usualHour)
            {
                var diff = Math.Abs(hour - usualHour);
                diff = Math.Min(diff, 24 - diff);
                if (diff >= 6)
                {
                    score += 10;
                    reasons.Add("Transfer time is unusual for your account");
                }
            }

            if (behavior.RiskScoreBaseline > 0)
            {
                score += Math.Min(behavior.RiskScoreBaseline, 10);
            }
        }

        if (input.TransferCountToday >= 3)
        {
            score += 20;
            reasons.Add("Multiple transfers today");
        }

        var note = input.Note?.ToLowerInvariant() ?? "";
        string[] riskyWords = ["urgent", "otp", "password", "investment", "guaranteed", "loan", "prize"];

        foreach (var word in riskyWords)
        {
            if (note.Contains(word))
            {
                score += 20;
                reasons.Add($"Suspicious keyword: {word}");
                break;
            }
        }

        var level = score >= 70 ? "High" : score >= 40 ? "Medium" : "Low";
        return new ScamRiskResult(Math.Min(score, 100), level, reasons);
    }
}
