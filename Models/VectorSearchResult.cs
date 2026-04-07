namespace SolitaAgent.Models;

public sealed record VectorSearchResult(
    string Answer,
    double Score,
    bool IsReliableMatch);
