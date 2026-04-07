using System.Text.Json.Serialization;

namespace SolitaAgent.Infrastructure.LlmProviders.Groq.Models;

public sealed class GroqChatRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("messages")]
    public required List<GroqChatMessage> Messages { get; init; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; init; }

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<GroqToolDefinition>? Tools { get; init; }

    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolChoice { get; init; }
}
