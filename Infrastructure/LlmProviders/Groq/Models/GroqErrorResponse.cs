using System.Text.Json.Serialization;

namespace SolitaAgent.Infrastructure.LlmProviders.Groq.Models;

public sealed class GroqErrorResponse
{
    [JsonPropertyName("error")]
    public GroqError? Error { get; init; }
}
