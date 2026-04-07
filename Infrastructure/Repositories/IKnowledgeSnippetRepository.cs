using SolitaAgent.Core.Models;

namespace SolitaAgent.Infrastructure.Repositories;

public interface IKnowledgeSnippetRepository
{
    IReadOnlyList<KnowledgeSnippet> GetAll();
}
