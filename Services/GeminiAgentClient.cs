using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;
using SolitaAgent.Contracts;
using SolitaAgent.Options;
using GeminiType = Google.GenAI.Types.Type;

namespace SolitaAgent.Services;

public sealed class GeminiAgentClient : IGeminiAgentClient
{
    private const string SystemInstruction = """
        You are the tool-selection agent for a small demo backend.

        You have exactly two tools:
        1. search_vector_knowledge: use this when the user's question can likely be answered from the local hardcoded knowledge base.
        2. get_predefined_response: use this when the question is outside the local knowledge base, too vague, or cannot be grounded safely.

        Rules:
        - Always call exactly one tool.
        - Never answer in natural language directly.
        - Do not call both tools.
        - Prefer get_predefined_response instead of guessing.
        - When calling search_vector_knowledge, pass the user's full original question as the query argument.
        """;

    private readonly GeminiOptions _options;

    public GeminiAgentClient(IOptions<GeminiOptions> options)
    {
        _options = options.Value;
    }

    public async Task<GeminiToolSelection> SelectToolAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new MissingGeminiApiKeyException();
        }

        var client = new Client(apiKey: _options.ApiKey, vertexAI: false);
        var response = await client.Models.GenerateContentAsync(
            model: _options.Model,
            contents: question,
            config: BuildConfig(),
            cancellationToken: cancellationToken);

        var functionCalls = response.FunctionCalls;
        if (functionCalls is null || functionCalls.Count != 1)
        {
            return GeminiToolSelection.Malformed();
        }

        var functionCall = functionCalls[0];
        if (string.IsNullOrWhiteSpace(functionCall.Name))
        {
            return GeminiToolSelection.Malformed();
        }

        if (string.Equals(
            functionCall.Name,
            AgentToolNames.SearchVectorKnowledge,
            StringComparison.Ordinal))
        {
            return TryGetStringArgument(functionCall.Args, "query", out var query)
                ? GeminiToolSelection.ForVectorSearch(query)
                : GeminiToolSelection.Malformed();
        }

        if (string.Equals(
            functionCall.Name,
            AgentToolNames.GetPredefinedResponse,
            StringComparison.Ordinal))
        {
            return GeminiToolSelection.ForStaticResponse();
        }

        return GeminiToolSelection.Malformed();
    }

    private static GenerateContentConfig BuildConfig()
    {
        return new GenerateContentConfig
        {
            SystemInstruction = new Content
            {
                Parts = [new Part { Text = SystemInstruction }]
            },
            Temperature = 0,
            Tools =
            [
                new Tool
                {
                    FunctionDeclarations =
                    [
                        BuildVectorKnowledgeDeclaration(),
                        BuildPredefinedResponseDeclaration()
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

    private static FunctionDeclaration BuildVectorKnowledgeDeclaration()
    {
        return new FunctionDeclaration
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
        };
    }

    private static FunctionDeclaration BuildPredefinedResponseDeclaration()
    {
        return new FunctionDeclaration
        {
            Name = AgentToolNames.GetPredefinedResponse,
            Description = "Return the predefined fallback response when a question cannot be grounded in the local knowledge base."
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
