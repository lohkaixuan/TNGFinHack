using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;

namespace ApiApp.AI;

public class BedrockAiService : IAiService
{
    private readonly IAmazonBedrockRuntime _bedrock;
    private readonly string _modelId;
    private readonly string? _guardrailId;
    private readonly string _guardrailVersion;

    public BedrockAiService(IConfiguration config)
    {
        var region = config["AWS:Region"] ?? "ap-southeast-1";

        _modelId = config["AWS:BedrockModelId"] ?? "amazon.nova-lite-v1:0";
        _guardrailId = config["AWS:GuardrailId"];
        _guardrailVersion = config["AWS:GuardrailVersion"] ?? "DRAFT";

        _bedrock = new AmazonBedrockRuntimeClient(
            RegionEndpoint.GetBySystemName(region)
        );
    }

    public async Task<string> AskAsync(string prompt)
    {
        var request = new ConverseRequest
        {
            ModelId = _modelId,
            Messages =
            [
                new Message
                {
                    Role = ConversationRole.User,
                    Content =
                    [
                        new ContentBlock { Text = prompt }
                    ]
                }
            ],
            InferenceConfig = new InferenceConfiguration
            {
                MaxTokens = 500,
                Temperature = 0.3F
            }
        };

        if (!string.IsNullOrWhiteSpace(_guardrailId))
        {
            request.GuardrailConfig = new GuardrailConfiguration
            {
                GuardrailIdentifier = _guardrailId,
                GuardrailVersion = _guardrailVersion,
                Trace = GuardrailTrace.Enabled
            };
        }

        var response = await _bedrock.ConverseAsync(request);

        return string.Join("\n",
            response.Output.Message.Content
                .Where(c => c.Text != null)
                .Select(c => c.Text)
        );
    }
}
