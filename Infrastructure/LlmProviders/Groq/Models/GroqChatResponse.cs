using System.Text.Json.Serialization;

namespace SolitaAgent.Infrastructure.LlmProviders.Groq.Models;

public sealed class GroqChatResponse
{
    [JsonPropertyName("choices")]
    public List<GroqChoice> Choices { get; init; } = [];
}
