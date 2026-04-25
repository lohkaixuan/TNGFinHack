namespace ApiApp.AI;

public record ScamRiskInput(
    decimal Amount,
    string ReceiverName,
    bool IsNewReceiver,
    int TransferCountToday,
    string? Note
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
