using System.Text.Json.Serialization;

namespace SolitaAgent.Infrastructure.LlmProviders.Groq.Models;

public sealed class GroqChoice
{
    [JsonPropertyName("message")]
    public GroqResponseMessage Message { get; init; } = new();

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; }
}
