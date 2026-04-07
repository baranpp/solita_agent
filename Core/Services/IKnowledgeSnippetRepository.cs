using SolitaAgent.Core.Models;

namespace SolitaAgent.Core.Services;

public interface IKnowledgeSnippetRepository
{
    IReadOnlyList<KnowledgeSnippet> GetAll();
}
