using SolitaAgent.Contracts;

namespace SolitaAgent.Services;

public sealed record GeminiToolSelection(string? ToolName, string? Query, bool IsMalformed)
{
    public static GeminiToolSelection ForVectorSearch(string query) =>
        new(AgentToolNames.SearchVectorKnowledge, query, false);

    public static GeminiToolSelection ForStaticResponse() =>
        new(AgentToolNames.GetPredefinedResponse, null, false);

    public static GeminiToolSelection Malformed() =>
        new(null, null, true);
}
