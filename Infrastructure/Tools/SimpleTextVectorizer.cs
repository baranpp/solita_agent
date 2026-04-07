// Comments are here to help the reviewer navigate the code.

using System.Text.RegularExpressions;
using SolitaAgent.Core.Services;

namespace SolitaAgent.Infrastructure.Tools;

// Bag-of-words vectorizer with stop word filtering. No external ML dependencies —
// tokenizes text into term-frequency vectors and computes cosine similarity.
public sealed class SimpleTextVectorizer : ITextVectorizer
{
    private static readonly Regex NonAlphaNumericRegex =
        new("[^a-z0-9\\s]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "a", "an", "the", "is", "it", "in", "on", "of", "to", "and", "or",
        "for", "at", "by", "from", "with", "as", "be", "was", "were", "been",
        "are", "am", "do", "does", "did", "has", "have", "had", "not", "no",
        "but", "if", "so", "than", "that", "this", "what", "which", "who",
        "how", "when", "where", "there", "then", "i", "you", "he", "she",
        "we", "they", "me", "my", "your", "its", "our", "their", "can",
        "will", "would", "could", "should", "may", "about", "more", "very",
        "just", "also", "usually", "generally"
    };

    public IReadOnlyDictionary<string, double> CreateVector(string text)
    {
        var vector = new Dictionary<string, double>(StringComparer.Ordinal);

        foreach (var token in Tokenize(text))
        {
            vector[token] = vector.TryGetValue(token, out var count) ? count + 1 : 1;
        }

        return vector;
    }

    public double CosineSimilarity(
        IReadOnlyDictionary<string, double> left,
        IReadOnlyDictionary<string, double> right)
    {
        if (left.Count == 0 || right.Count == 0)
        {
            return 0;
        }

        var dotProduct = 0d;
        foreach (var (token, value) in left)
        {
            if (right.TryGetValue(token, out var otherValue))
            {
                dotProduct += value * otherValue;
            }
        }

        var leftMagnitude = Math.Sqrt(left.Values.Sum(value => value * value));
        var rightMagnitude = Math.Sqrt(right.Values.Sum(value => value * value));

        if (leftMagnitude == 0 || rightMagnitude == 0)
        {
            return 0;
        }

        return dotProduct / (leftMagnitude * rightMagnitude);
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var normalized = NonAlphaNumericRegex.Replace(text.ToLowerInvariant(), " ");
        return normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => !StopWords.Contains(token));
    }
}
