using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;
using SolitaAgent.Core.Contracts;
using SolitaAgent.Core.Services;
using SolitaAgent.Infrastructure.LlmProviders.Gemini.Exceptions;
using GeminiType = Google.GenAI.Types.Type;

namespace SolitaAgent.Infrastructure.LlmProviders.Gemini;

public sealed class GeminiClient : IToolSelectionClient, IAnswerGenerationClient
{
    private const string ToolSelectionInstruction = """
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

    private const string AnswerGenerationInstruction = """
        You are a helpful assistant that answers user questions based on tool results
        from a local knowledge base.

        You will receive:
        - The user's original question.
        - The name of the tool that was used.
        - The output of that tool.
        - Optionally, a similarity score (0 to 1) indicating how well the retrieved
          snippet matched the question.

        Rules:
        - If a knowledge base snippet was retrieved with a reasonable similarity score,
          use it to answer the question naturally and conversationally.
        - If the similarity score is low or the snippet does not clearly answer the
          question, acknowledge what you found but be honest that the information may
          not fully address their question.
        - If the fallback tool was used, politely tell the user that their question
          could not be answered from the available knowledge base.
        - Keep answers concise — one to three sentences.
        - Do not invent facts beyond what the tool output provides.
        """;

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
            ToolSelectionInstruction,
            BuildToolSelectionConfig(),
            cancellationToken);

        return ParseToolSelection(response);
    }

    public async Task<string> GenerateAnswerAsync(
        string question,
        string toolName,
        string toolOutput,
        double? similarityScore,
        CancellationToken cancellationToken = default)
    {
        EnsureApiKeyConfigured();

        var prompt = BuildAnswerPrompt(question, toolName, toolOutput, similarityScore);

        var response = await GenerateContentAsync(
            prompt,
            AnswerGenerationInstruction,
            new GenerateContentConfig { Temperature = 0 },
            cancellationToken);

        return response.Text ?? toolOutput;
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

        var client = new Client(apiKey: _options.ApiKey, vertexAI: false);
        return await client.Models.GenerateContentAsync(
            model: _options.Model,
            contents: contents,
            config: config,
            cancellationToken: cancellationToken);
    }

    private void EnsureApiKeyConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new MissingGeminiApiKeyException();
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
        if (string.IsNullOrWhiteSpace(functionCall.Name))
        {
            return ToolSelectionResult.Malformed();
        }

        if (string.Equals(
            functionCall.Name,
            AgentToolNames.SearchVectorKnowledge,
            StringComparison.Ordinal))
        {
            return TryGetStringArgument(functionCall.Args, "query", out var query)
                ? ToolSelectionResult.ForVectorSearch(query)
                : ToolSelectionResult.Malformed();
        }

        if (string.Equals(
            functionCall.Name,
            AgentToolNames.GetPredefinedResponse,
            StringComparison.Ordinal))
        {
            return ToolSelectionResult.ForStaticResponse();
        }

        return ToolSelectionResult.Malformed();
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

    private static string BuildAnswerPrompt(
        string question,
        string toolName,
        string toolOutput,
        double? similarityScore)
    {
        var scoreInfo = similarityScore.HasValue
            ? $"\nSimilarity score: {similarityScore.Value:F2}"
            : string.Empty;

        return $"""
            User question: {question}
            Tool used: {toolName}
            Tool output: {toolOutput}{scoreInfo}
            """;
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
