using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SolitaAgent.Contracts;
using SolitaAgent.Models.Groq;
using SolitaAgent.Options;

namespace SolitaAgent.Services;

public sealed class GroqToolSelectionClient : IToolSelectionClient
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

    public GroqToolSelectionClient(
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
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new MissingGroqApiKeyException();
        }


        var request = new GroqChatRequest
        {
            Model = _options.Model,
            Messages =
            [
                new GroqChatMessage { Role = "system", Content = SystemInstruction },
                new GroqChatMessage { Role = "user", Content = question }
            ],
            Temperature = 0,
            Tools = ToolDefinitions,
            ToolChoice = "required"
        };

        var response = await SendRequestAsync(request, cancellationToken);

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
            throw new GroqApiException(httpResponse.StatusCode, message);
        }

        return await httpResponse.Content.ReadFromJsonAsync<GroqChatResponse>(
            cancellationToken: cancellationToken)
            ?? throw new GroqApiException(System.Net.HttpStatusCode.InternalServerError, "Groq returned an empty response.");
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
