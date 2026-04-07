using System.Text.Json.Serialization;

namespace SolitaAgent.Infrastructure.LlmProviders.Groq.Models;

public sealed class GroqError
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
}
