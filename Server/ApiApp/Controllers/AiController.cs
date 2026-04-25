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
                aws_error = ex.ErrorCode
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
