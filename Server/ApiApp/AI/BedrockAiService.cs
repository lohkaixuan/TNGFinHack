using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;

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
        var bedrockToken = config["AWS:BedrockToken"];
        _modelId = config["AWS:BedrockModelId"] ?? "amazon.nova-lite-v1:0";
        _guardrailId = config["AWS:GuardrailId"];
        _guardrailVersion = config["AWS:GuardrailVersion"] ?? "DRAFT";

        var regionEndpoint = RegionEndpoint.GetBySystemName(region);
        var accessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        var sessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");

        if (!string.IsNullOrWhiteSpace(bedrockToken))
        {
            var configBedrock = new AmazonBedrockRuntimeConfig { RegionEndpoint = regionEndpoint };
            _bedrock = new AmazonBedrockRuntimeClient(new AnonymousAWSCredentials(), configBedrock);
            
            ((AmazonServiceClient)_bedrock).BeforeRequestEvent += (sender, e) =>
            {
                if (e is Amazon.Runtime.WebServiceRequestEventArgs args)
                {
                    // The specific token is a CallWithBearerToken string, so it expects be placed in the Authorization header.
                    args.Headers["Authorization"] = $"Bearer {bedrockToken}";
                }
            };
        }
        else
        {
            _bedrock = !string.IsNullOrWhiteSpace(accessKeyId) &&
                       !string.IsNullOrWhiteSpace(secretAccessKey) &&
                       !string.IsNullOrWhiteSpace(sessionToken)
                ? new AmazonBedrockRuntimeClient(
                    new SessionAWSCredentials(accessKeyId, secretAccessKey, sessionToken),
                    regionEndpoint)
                : new AmazonBedrockRuntimeClient(regionEndpoint);
        }
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
                MaxTokens = 512,
                Temperature = 0.7F,
                TopP = 0.9F,
                StopSequences = []
            },
            AdditionalModelRequestFields = Amazon.Runtime.Documents.Document.FromObject(new Dictionary<string, object>())
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
