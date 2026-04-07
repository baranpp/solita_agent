namespace SolitaAgent.Infrastructure.LlmProviders.Gemini;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";
    public const string DefaultModel = "gemini-2.5-flash";

    public string Model { get; set; } = DefaultModel;

    public string? ApiKey { get; set; }
}
