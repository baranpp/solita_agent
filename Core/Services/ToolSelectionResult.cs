using SolitaAgent.Core.Contracts;

namespace SolitaAgent.Core.Services;

public sealed record ToolSelectionResult(string? ToolName, string? Query, bool IsMalformed)
{
    public static ToolSelectionResult ForVectorSearch(string query) =>
        new(AgentToolNames.SearchVectorKnowledge, query, false);

    public static ToolSelectionResult ForStaticResponse() =>
        new(AgentToolNames.GetPredefinedResponse, null, false);

    public static ToolSelectionResult Malformed() =>
        new(null, null, true);
}
