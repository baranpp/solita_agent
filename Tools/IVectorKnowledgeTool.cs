using SolitaAgent.Models;

namespace SolitaAgent.Tools;

public interface IVectorKnowledgeTool
{
    string Name { get; }

    VectorSearchResult Search(string query);
}
