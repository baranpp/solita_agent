// Comments are here to help the reviewer navigate the code.

using SolitaAgent.Api.Exceptions;

namespace SolitaAgent.Api.Validators;

// Api-layer input validation (null/empty, length).
// Separated from Core's QuestionHeuristicValidator which handles domain-level validation
// (is-it-a-question). Each layer validates its own concerns.

public sealed class InputSanitizationValidator : IInputSanitizer
{
    private const int MaxQuestionLength = 500;

    public string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new InputValidationException(
                "Provide a non-empty 'question' query string parameter.");
        }

        var trimmed = input.Trim();

        if (trimmed.Length > MaxQuestionLength)
        {
            throw new InputValidationException(
                $"Question must not exceed {MaxQuestionLength} characters.");
        }

        return trimmed;
    }
}
