using SolitaAgent.Core.Models;

namespace SolitaAgent.Core.Services;

public interface IVectorKnowledgeTool
{
    string Name { get; }

    VectorSearchResult Search(string query);
}
