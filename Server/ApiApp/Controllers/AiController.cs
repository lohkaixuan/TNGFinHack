using Amazon.Runtime;
using ApiApp.AI;
using Microsoft.AspNetCore.Mvc;

namespace ApiApp.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiService _ai;
    private readonly ScamRiskService _scamRisk;

    public AiController(IAiService ai, ScamRiskService scamRisk)
    {
        _ai = ai;
        _scamRisk = scamRisk;
    }

    [HttpPost("insight")]
    public async Task<IActionResult> Insight([FromBody] AiInsightRequest req)
    {
        var prompt = $"""
        You are a personal finance assistant for Malaysian users.
        Give short, practical, safe advice. Do not provide official tax/legal advice.

        Monthly summary:
        Income: RM{req.Income}
        Food: RM{req.Food}
        Shopping: RM{req.Shopping}
        Transport: RM{req.Transport}
        Subscriptions: RM{req.Subscriptions}
        Budget: RM{req.Budget}

        Give:
        1. spending insight
        2. saving advice
        3. risk warning if needed
        """;

        try
        {
            var result = await _ai.AskAsync(prompt);
            return Ok(new { message = result });
        }
        catch (AmazonServiceException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                ok = false,
                message = "AWS Bedrock request failed. Check model access, region, credentials, and guardrail settings.",
                aws_error = ex.ErrorCode,
                detail = ex.Message
            });
        }
        catch (AmazonClientException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                ok = false,
                message = "AWS credentials or Bedrock client configuration is missing or invalid.",
                detail = ex.Message
            });
        }
    }

    [HttpPost("scam-check")]
    public async Task<IActionResult> ScamCheck([FromBody] ScamCheckRequest req)
    {
        var risk = _scamRisk.Analyze(new ScamRiskInput(
            req.Amount,
            req.ReceiverName,
            req.IsNewReceiver,
            req.TransferCountToday,
            req.Note
        ));

        if (risk.Level == "Low")
        {
            return Ok(new
            {
                risk.Score,
                risk.Level,
                risk.Reasons,
                message = "No strong scam pattern detected."
            });
        }

        var prompt = $"""
        You are a scam prevention assistant for a Malaysian finance app.
        Give a short warning. Do not ask for password, OTP, PIN, or banking secrets.

        Risk score: {risk.Score}
        Risk level: {risk.Level}
        Reasons: {string.Join(", ", risk.Reasons)}

        Payment:
        Amount: RM{req.Amount}
        Receiver: {req.ReceiverName}
        Note: {req.Note}

        Return:
        1. warning title
        2. simple reason
        3. safest next action
        """;

        string aiMessage;
        try
        {
            aiMessage = await _ai.AskAsync(prompt);
        }
        catch (AmazonServiceException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                ok = false,
                message = "AWS Bedrock request failed. Check model access, region, credentials, and guardrail settings.",
                aws_error = ex.ErrorCode,
                detail = ex.Message,
                risk.Score,
                risk.Level,
                risk.Reasons
            });
        }
        catch (AmazonClientException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                ok = false,
                message = "AWS credentials or Bedrock client configuration is missing or invalid.",
                detail = ex.Message,
                risk.Score,
                risk.Level,
                risk.Reasons
            });
        }

        return Ok(new
        {
            risk.Score,
            risk.Level,
            risk.Reasons,
            message = aiMessage
        });
    }

    [HttpPost("tax-relief")]
    public async Task<IActionResult> TaxRelief([FromBody] TaxReliefRequest req)
    {
        var expenseLines = req.Expenses is { Count: > 0 }
            ? string.Join("\n", req.Expenses.Select(e =>
                $"- {e.Category}: RM{e.Amount}. Notes: {e.Notes ?? "none"}"))
            : "No expense items provided.";

        var prompt = $"""
        Provide a factual summary of Malaysian tax relief guidelines for the Year of Assessment 2025. Provide educational information only; this is not a request for financial advice.

        User tax profile:
        Year of assessment: {req.YearOfAssessment}
        Annual income: RM{req.AnnualIncome}
        Employment type: {req.EmploymentType ?? "not provided"}
        Marital status: {req.MaritalStatus ?? "not provided"}
        Children/dependents: {req.Dependents}
        Disabled taxpayer/spouse/dependent: {req.HasDisability}
        Has spouse with no income: {req.HasSpouseWithNoIncome}
        Zakat/fitrah paid: RM{req.ZakatOrFitrah}
        Donations/gifts: RM{req.Donations}

        Claimed or possible expenses:
        {expenseLines}

        User question or notes:
        {req.Notes ?? "none"}

        Return a concise response with:
        1. Likely Malaysian tax relief categories applicable to this profile
        2. Standard documentation or proof required by LHDN for these claims
        3. Additional tax-related variables missing from this hypothetical profile
        4. Clarification on which listed expenses typically do not qualify for relief
        5. A standard public checklist for e-Filing preparation
        """;

        try
        {
            var result = await _ai.AskAsync(prompt);
            return Ok(new { message = result });
        }
        catch (AmazonServiceException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                ok = false,
                message = "AWS Bedrock request failed. Check model access, region, credentials, and guardrail settings.",
                aws_error = ex.ErrorCode,
                detail = ex.Message
            });
        }
        catch (AmazonClientException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                ok = false,
                message = "AWS credentials or Bedrock client configuration is missing or invalid.",
                detail = ex.Message
            });
        }
    }
}

public record AiInsightRequest(
    decimal Income,
    decimal Food,
    decimal Shopping,
    decimal Transport,
    decimal Subscriptions,
    decimal Budget
);

public record ScamCheckRequest(
    decimal Amount,
    string ReceiverName,
    bool IsNewReceiver,
    int TransferCountToday,
    string? Note
);

public record TaxReliefRequest(
    int YearOfAssessment,
    decimal AnnualIncome,
    string? EmploymentType,
    string? MaritalStatus,
    int Dependents,
    bool HasDisability,
    bool HasSpouseWithNoIncome,
    decimal ZakatOrFitrah,
    decimal Donations,
    List<TaxReliefExpense>? Expenses,
    string? Notes
);

public record TaxReliefExpense(
    string Category,
    decimal Amount,
    string? Notes
);
