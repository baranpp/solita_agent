namespace SolitaAgent.Core.Options;

public sealed class VectorSearchOptions
{
    public const string SectionName = "VectorSearch";
    public const double DefaultSimilarityThreshold = 0.30;

    public double SimilarityThreshold { get; set; } = DefaultSimilarityThreshold;
}
