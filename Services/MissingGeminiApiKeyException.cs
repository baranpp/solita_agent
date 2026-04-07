namespace SolitaAgent.Services;

public sealed class MissingGeminiApiKeyException : InvalidOperationException
{
    public MissingGeminiApiKeyException()
        : base("The GEMINI_API_KEY environment variable is not configured.")
    {
    }
}
