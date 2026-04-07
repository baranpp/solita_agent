// Comments are here to help the reviewer navigate the code.

using SolitaAgent.Core.Models;
using SolitaAgent.Core.Services;

namespace SolitaAgent.Infrastructure.Repositories;

// Hardcoded knowledge base for the demo. In production this would be backed by a
// real data store and the interface (IKnowledgeSnippetRepository) would stay the same.
public sealed class InMemoryKnowledgeSnippetRepository : IKnowledgeSnippetRepository
{
    private static readonly IReadOnlyList<KnowledgeSnippet> Snippets = new List<KnowledgeSnippet>
    {
        new("france-sweden", "It is warmer in France than Sweden."),
        new("korea-finland", "It is warmer in Korea than Finland."),
        new("spain-norway", "Spain is usually warmer than Norway."),
        new("italy-denmark", "Italy is generally warmer than Denmark."),
        new("portugal-iceland", "Portugal is warmer than Iceland."),
        new("greece-estonia", "Greece is warmer than Estonia."),
        new("turkey-sweden", "Turkey is warmer than Sweden."),
        new("spain-germany", "Spain is usually warmer than Germany.")
    };

    public IReadOnlyList<KnowledgeSnippet> GetAll() => Snippets;
}
