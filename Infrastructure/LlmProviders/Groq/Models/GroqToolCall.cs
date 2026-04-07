using System.Text.Json.Serialization;

namespace SolitaAgent.Infrastructure.LlmProviders.Groq.Models;

public sealed class GroqToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    [JsonPropertyName("function")]
    public GroqFunctionCall Function { get; init; } = new();
}
