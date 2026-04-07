namespace SolitaAgent.Core.Exceptions;

public enum LlmErrorKind
{
    ApiKeyMissing,
    ProviderUnavailable,
    ProviderRejected
}

public sealed class LlmProviderException : Exception
{
    public LlmErrorKind Kind { get; }

    public LlmProviderException(LlmErrorKind kind, string message, Exception? inner = null)
        : base(message, inner)
    {
        Kind = kind;
    }
}
