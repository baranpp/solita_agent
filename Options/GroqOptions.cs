namespace SolitaAgent.Options;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";
    public const string DefaultModel = "llama-3.3-70b-versatile";

    public string Model { get; set; } = DefaultModel;
    public string? ApiKey { get; set; }
}
