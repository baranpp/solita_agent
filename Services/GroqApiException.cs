using System.Net;

namespace SolitaAgent.Services;

public sealed class GroqApiException : HttpRequestException
{
    public GroqApiException(HttpStatusCode statusCode, string message)
        : base(message, inner: null, statusCode)
    {
    }
}
