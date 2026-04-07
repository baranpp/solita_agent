using System.Net;

namespace SolitaAgent.Infrastructure.LlmProviders.Groq.Exceptions;

public sealed class GroqApiException : HttpRequestException
{
    public GroqApiException(HttpStatusCode statusCode, string message)
        : base(message, inner: null, statusCode)
    {
    }
}
