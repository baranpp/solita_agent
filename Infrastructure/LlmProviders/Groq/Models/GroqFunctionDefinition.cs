using System.Text.Json.Serialization;

namespace SolitaAgent.Infrastructure.LlmProviders.Groq.Models;

public sealed class GroqFunctionDefinition
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Parameters { get; init; }
}
