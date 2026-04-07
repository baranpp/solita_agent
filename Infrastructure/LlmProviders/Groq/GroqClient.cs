using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SolitaAgent.Core.Contracts;
using SolitaAgent.Core.Exceptions;
using SolitaAgent.Core.Prompts;
using SolitaAgent.Core.Services;
using SolitaAgent.Infrastructure.LlmProviders.Groq.Models;

namespace SolitaAgent.Infrastructure.LlmProviders.Groq;

public sealed class GroqClient : IToolSelectionClient, IAnswerGenerationClient
{
    private static readonly List<GroqToolDefinition> ToolDefinitions =
    [
        new GroqToolDefinition
        {
            Function = new GroqFunctionDefinition
            {
                Name = AgentToolNames.SearchVectorKnowledge,
                Description = "Search the local in-memory knowledge base of hardcoded text snippets.",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new
                        {
                            type = "string",
                            description = "The user's original question."
                        }
                    },
                    required = new[] { "query" }
                }
            }
        },
        new GroqToolDefinition
        {
            Function = new GroqFunctionDefinition
            {
                Name = AgentToolNames.GetPredefinedResponse,
                Description = "Return the predefined fallback response when a question cannot be grounded in the local knowledge base."
            }
        }
    ];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GroqOptions _options;

    public GroqClient(
        IHttpClientFactory httpClientFactory,
        IOptions<GroqOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<ToolSelectionResult> SelectToolAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        EnsureApiKeyConfigured();

        var request = new GroqChatRequest
        {
            Model = _options.Model,
            Messages =
            [
                new GroqChatMessage { Role = "system", Content = AgentPrompts.ToolSelection },
                new GroqChatMessage { Role = "user", Content = question }
            ],
            Temperature = 0,
            Tools = ToolDefinitions,
            ToolChoice = "required"
        };

        var response = await SendRequestAsync(request, cancellationToken);

        return ParseToolSelection(response);
    }

    public async Task<string> GenerateAnswerAsync(
        AnswerGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureApiKeyConfigured();

        var prompt = BuildAnswerPrompt(request);

        var chatRequest = new GroqChatRequest
        {
            Model = _options.Model,
            Messages =
            [
                new GroqChatMessage { Role = "system", Content = AgentPrompts.AnswerGeneration },
                new GroqChatMessage { Role = "user", Content = prompt }
            ],
            Temperature = 0
        };

        var response = await SendRequestAsync(chatRequest, cancellationToken);

        return response.Choices.FirstOrDefault()?.Message.Content ?? request.ToolOutput;
    }

    private async Task<GroqChatResponse> SendRequestAsync(
        GroqChatRequest request,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("Groq");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", _options.ApiKey);
        httpRequest.Content = JsonContent.Create(request);

        using var httpResponse = await client.SendAsync(httpRequest, cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadFromJsonAsync<GroqErrorResponse>(
                cancellationToken: cancellationToken);
            var message = errorBody?.Error?.Message ?? "Groq API request failed.";
            var kind = (int)httpResponse.StatusCode >= 500
                ? LlmErrorKind.ProviderUnavailable
                : LlmErrorKind.ProviderRejected;
            throw new LlmProviderException(kind, message);
        }

        return await httpResponse.Content.ReadFromJsonAsync<GroqChatResponse>(
            cancellationToken: cancellationToken)
            ?? throw new LlmProviderException(LlmErrorKind.ProviderUnavailable, "Groq returned an empty response.");
    }

    private void EnsureApiKeyConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new LlmProviderException(
                LlmErrorKind.ApiKeyMissing,
                "The GROQ_API_KEY environment variable is not configured.");
        }
    }

    private static ToolSelectionResult ParseToolSelection(GroqChatResponse response)
    {
        var toolCalls = response.Choices.FirstOrDefault()?.Message.ToolCalls;
        if (toolCalls is null || toolCalls.Count != 1)
        {
            return ToolSelectionResult.Malformed();
        }

        var toolCall = toolCalls[0];
        if (string.IsNullOrWhiteSpace(toolCall.Function.Name))
        {
            return ToolSelectionResult.Malformed();
        }

        if (string.Equals(
            toolCall.Function.Name,
            AgentToolNames.SearchVectorKnowledge,
            StringComparison.Ordinal))
        {
            return TryParseQuery(toolCall.Function.Arguments, out var query)
                ? ToolSelectionResult.ForVectorSearch(query)
                : ToolSelectionResult.Malformed();
        }

        if (string.Equals(
            toolCall.Function.Name,
            AgentToolNames.GetPredefinedResponse,
            StringComparison.Ordinal))
        {
            return ToolSelectionResult.ForStaticResponse();
        }

        return ToolSelectionResult.Malformed();
    }

    private static string BuildAnswerPrompt(AnswerGenerationRequest request)
    {
        var scoreInfo = request.SimilarityScore.HasValue
            ? $"\nSimilarity score: {request.SimilarityScore.Value:F2}"
            : string.Empty;

        return $"""
            User question: {request.Question}
            Tool used: {request.ToolName}
            Tool output: {request.ToolOutput}{scoreInfo}
            """;
    }

    private static bool TryParseQuery(string argumentsJson, out string query)
    {
        query = string.Empty;

        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(argumentsJson);
            if (document.RootElement.TryGetProperty("query", out var queryElement) &&
                queryElement.ValueKind == JsonValueKind.String)
            {
                var value = queryElement.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    query = value;
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }
}
