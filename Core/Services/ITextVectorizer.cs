namespace SolitaAgent.Core.Services;

public interface ITextVectorizer
{
    IReadOnlyDictionary<string, double> CreateVector(string text);

    double CosineSimilarity(
        IReadOnlyDictionary<string, double> left,
        IReadOnlyDictionary<string, double> right);
}
