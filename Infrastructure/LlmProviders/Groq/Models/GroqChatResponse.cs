using System.Text.Json.Serialization;

namespace SolitaAgent.Infrastructure.LlmProviders.Groq.Models;

public sealed class GroqChatResponse
{
    [JsonPropertyName("choices")]
    public List<GroqChoice> Choices { get; init; } = [];
}

public sealed class GroqChoice
{
    [JsonPropertyName("message")]
    public GroqResponseMessage Message { get; init; } = new();

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; }
}

public sealed class GroqResponseMessage
{
    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("tool_calls")]
    public List<GroqToolCall>? ToolCalls { get; init; }
}

public sealed class GroqToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    [JsonPropertyName("function")]
    public GroqFunctionCall Function { get; init; } = new();
}

public sealed class GroqFunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; init; } = string.Empty;
}

public sealed class GroqErrorResponse
{
    [JsonPropertyName("error")]
    public GroqError? Error { get; init; }
}

public sealed class GroqError
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
}
