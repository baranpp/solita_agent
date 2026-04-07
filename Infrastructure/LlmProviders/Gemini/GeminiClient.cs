using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;
using SolitaAgent.Core.Contracts;
using SolitaAgent.Core.Exceptions;
using SolitaAgent.Core.Prompts;
using SolitaAgent.Core.Services;
using GeminiType = Google.GenAI.Types.Type;

namespace SolitaAgent.Infrastructure.LlmProviders.Gemini;

public sealed class GeminiClient : IToolSelectionClient, IAnswerGenerationClient
{
    private readonly GeminiOptions _options;

    public GeminiClient(IOptions<GeminiOptions> options)
    {
        _options = options.Value;
    }

    public async Task<ToolSelectionResult> SelectToolAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        EnsureApiKeyConfigured();

        var response = await GenerateContentAsync(
            question,
            AgentPrompts.ToolSelection,
            BuildToolSelectionConfig(),
            cancellationToken);

        return ParseToolSelection(response);
    }

    public async Task<string> GenerateAnswerAsync(
        AnswerGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureApiKeyConfigured();

        var prompt = AnswerPromptBuilder.Build(request);

        var response = await GenerateContentAsync(
            prompt,
            AgentPrompts.AnswerGeneration,
            new GenerateContentConfig { Temperature = 0 },
            cancellationToken);

        return response.Text ?? request.ToolOutput;
    }

    private async Task<GenerateContentResponse> GenerateContentAsync(
        string contents,
        string systemInstruction,
        GenerateContentConfig config,
        CancellationToken cancellationToken)
    {
        config.SystemInstruction = new Content
        {
            Parts = [new Part { Text = systemInstruction }]
        };

        try
        {
            var client = new Client(apiKey: _options.ApiKey, vertexAI: false);
            return await client.Models.GenerateContentAsync(
                model: _options.Model,
                contents: contents,
                config: config,
                cancellationToken: cancellationToken);
        }
        catch (ClientError ex)
        {
            throw new LlmProviderException(LlmErrorKind.ProviderRejected, ex.Message, ex);
        }
        catch (ServerError ex)
        {
            throw new LlmProviderException(LlmErrorKind.ProviderUnavailable, ex.Message, ex);
        }
    }

    private void EnsureApiKeyConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new LlmProviderException(
                LlmErrorKind.ApiKeyMissing,
                "The GEMINI_API_KEY environment variable is not configured.");
        }
    }

    private static ToolSelectionResult ParseToolSelection(GenerateContentResponse response)
    {
        var functionCalls = response.FunctionCalls;
        if (functionCalls is null || functionCalls.Count != 1)
        {
            return ToolSelectionResult.Malformed();
        }

        var functionCall = functionCalls[0];

        return ToolSelectionParser.Route(
            functionCall.Name,
            () => TryGetStringArgument(functionCall.Args, "query", out var q) ? q : null);
    }

    private static GenerateContentConfig BuildToolSelectionConfig()
    {
        return new GenerateContentConfig
        {
            Temperature = 0,
            Tools =
            [
                new Tool
                {
                    FunctionDeclarations =
                    [
                        new FunctionDeclaration
                        {
                            Name = AgentToolNames.SearchVectorKnowledge,
                            Description = "Search the local in-memory knowledge base of hardcoded text snippets.",
                            Parameters = new Schema
                            {
                                Type = GeminiType.Object,
                                Properties = new Dictionary<string, Schema>
                                {
                                    ["query"] = new()
                                    {
                                        Type = GeminiType.String,
                                        Description = "The user's original question."
                                    }
                                },
                                Required = ["query"]
                            }
                        },
                        new FunctionDeclaration
                        {
                            Name = AgentToolNames.GetPredefinedResponse,
                            Description = "Return the predefined fallback response when a question cannot be grounded in the local knowledge base."
                        }
                    ]
                }
            ],
            ToolConfig = new ToolConfig
            {
                FunctionCallingConfig = new FunctionCallingConfig
                {
                    Mode = FunctionCallingConfigMode.Any,
                    AllowedFunctionNames =
                    [
                        AgentToolNames.SearchVectorKnowledge,
                        AgentToolNames.GetPredefinedResponse
                    ]
                }
            }
        };
    }

    private static bool TryGetStringArgument(
        Dictionary<string, object>? args,
        string key,
        out string value)
    {
        value = string.Empty;

        if (args is null || !args.TryGetValue(key, out var rawValue) || rawValue is null)
        {
            return false;
        }

        switch (rawValue)
        {
            case string text when !string.IsNullOrWhiteSpace(text):
                value = text;
                return true;
            case JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.String:
                var jsonText = jsonElement.GetString();
                if (!string.IsNullOrWhiteSpace(jsonText))
                {
                    value = jsonText;
                    return true;
                }

                break;
        }

        return false;
    }
}
