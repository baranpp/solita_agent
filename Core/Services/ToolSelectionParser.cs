// Comments are here to help the reviewer navigate the code.

using SolitaAgent.Core.Contracts;

namespace SolitaAgent.Core.Services;

// Shared routing logic extracted from both LLM clients to avoid duplication (DRY).
// Each provider still handles its own response parsing; this class handles the
// common decision: given a tool name, which ToolSelectionResult to return.
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
