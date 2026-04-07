namespace SolitaAgent.Core.Exceptions;

public sealed class QuestionValidationException : Exception
{
    public QuestionValidationKind Kind { get; }

    public QuestionValidationException(QuestionValidationKind kind, string message)
        : base(message)
    {
        Kind = kind;
    }
}
