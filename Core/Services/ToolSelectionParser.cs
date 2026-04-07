using SolitaAgent.Core.Contracts;

namespace SolitaAgent.Core.Services;

public static class ToolSelectionParser
{
    public static ToolSelectionResult Route(string? toolName, Func<string?> tryExtractQuery)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return ToolSelectionResult.Malformed();
        }

        if (string.Equals(toolName, AgentToolNames.SearchVectorKnowledge, StringComparison.Ordinal))
        {
            var query = tryExtractQuery();
            return !string.IsNullOrWhiteSpace(query)
                ? ToolSelectionResult.ForVectorSearch(query)
                : ToolSelectionResult.Malformed();
        }

        if (string.Equals(toolName, AgentToolNames.GetPredefinedResponse, StringComparison.Ordinal))
        {
            return ToolSelectionResult.ForStaticResponse();
        }

        return ToolSelectionResult.Malformed();
    }
}
