namespace SolitaAgent.Services;

public sealed class MissingGroqApiKeyException : InvalidOperationException
{
    public MissingGroqApiKeyException()
        : base("The GROQ_API_KEY environment variable is not configured.")
    {
    }
}
