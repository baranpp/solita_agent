// Comments are here to help the reviewer navigate the code.

using Microsoft.Extensions.Options;
using SolitaAgent.Core.Contracts;
using SolitaAgent.Core.Models;
using SolitaAgent.Core.Options;
using SolitaAgent.Core.Services;

namespace SolitaAgent.Infrastructure.Tools;

// Implements the vector search tool. Pre-indexes all knowledge snippets at construction
// time so searches are a single pass over the index.
public sealed class VectorKnowledgeTool : IVectorKnowledgeTool
{
    private readonly IReadOnlyList<IndexedSnippet> _indexedSnippets;
    private readonly ITextVectorizer _vectorizer;
    private readonly double _similarityThreshold;

    public VectorKnowledgeTool(
        IKnowledgeSnippetRepository knowledgeSnippetRepository,
        ITextVectorizer vectorizer,
        IOptions<VectorSearchOptions> options)
    {
        _vectorizer = vectorizer;
        _similarityThreshold = options.Value.SimilarityThreshold;
        _indexedSnippets = knowledgeSnippetRepository
            .GetAll()
            .Select(snippet => new IndexedSnippet(snippet, _vectorizer.CreateVector(snippet.Text)))
            .ToList();
    }

    public string Name => AgentToolNames.SearchVectorKnowledge;

    // Brute-force cosine similarity over all snippets. Sufficient for a small in-memory dataset.
    public VectorSearchResult Search(string query)
    {
        if (_indexedSnippets.Count == 0)
        {
            return new VectorSearchResult(string.Empty, 0, false);
        }

        var queryVector = _vectorizer.CreateVector(query);
        if (queryVector.Count == 0)
        {
            return new VectorSearchResult(string.Empty, 0, false);
        }

        IndexedSnippet? bestMatch = null;
        var bestScore = -1d;

        foreach (var indexedSnippet in _indexedSnippets)
        {
            var score = _vectorizer.CosineSimilarity(queryVector, indexedSnippet.Vector);
            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = indexedSnippet;
            }
        }

        if (bestMatch is null)
        {
            return new VectorSearchResult(string.Empty, 0, false);
        }

        return new VectorSearchResult(
            bestMatch.Snippet.Text,
            bestScore,
            bestScore >= _similarityThreshold);
    }

    private sealed record IndexedSnippet(
        KnowledgeSnippet Snippet,
        IReadOnlyDictionary<string, double> Vector);
}
