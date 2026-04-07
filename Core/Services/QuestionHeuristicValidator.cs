using SolitaAgent.Core.Exceptions;

namespace SolitaAgent.Core.Services;

public sealed class QuestionHeuristicValidator : IQuestionValidator
{
    private static readonly string[] QuestionPrefixes =
    [
        "who", "what", "where", "when", "why", "how",
        "is", "are", "was", "were",
        "do", "does", "did",
        "can", "could", "will", "would", "should",
        "shall", "may", "might",
        "have", "has", "had",
        "tell", "explain", "describe"
    ];

    public void Validate(string question)
    {
        if (EndsWithQuestionMark(question) || StartsWithQuestionWord(question))
        {
            return;
        }

        throw new QuestionValidationException(
            QuestionValidationKind.NotAQuestion,
            "The input does not appear to be a question. "
            + "Please rephrase as a question or end with '?'.");
    }

    private static bool EndsWithQuestionMark(string question)
    {
        return question.EndsWith('?');
    }

    private static bool StartsWithQuestionWord(string question)
    {
        foreach (var prefix in QuestionPrefixes)
        {
            if (question.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && question.Length > prefix.Length
                && !char.IsLetter(question[prefix.Length]))
            {
                return true;
            }
        }

        return false;
    }
}
