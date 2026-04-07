using SolitaAgent.Core.Models;

namespace SolitaAgent.Infrastructure.Tools;

public interface IVectorKnowledgeTool
{
    string Name { get; }

    VectorSearchResult Search(string query);
}
