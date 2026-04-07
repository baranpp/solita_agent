using SolitaAgent.Models;

namespace SolitaAgent.Repositories;

public interface IKnowledgeSnippetRepository
{
    IReadOnlyList<KnowledgeSnippet> GetAll();
}
