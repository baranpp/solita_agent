using SolitaAgent.Contracts;
using SolitaAgent.Tools;

namespace SolitaAgent.Services;

public sealed class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IGeminiAgentClient _geminiAgentClient;
    private readonly IVectorKnowledgeTool _vectorKnowledgeTool;
    private readonly IStaticResponseTool _staticResponseTool;

    public AgentOrchestrator(
        IGeminiAgentClient geminiAgentClient,
        IVectorKnowledgeTool vectorKnowledgeTool,
        IStaticResponseTool staticResponseTool)
    {
        _geminiAgentClient = geminiAgentClient;
        _vectorKnowledgeTool = vectorKnowledgeTool;
        _staticResponseTool = staticResponseTool;
    }

    public async Task<AskResponse> AskAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        var selection = await _geminiAgentClient.SelectToolAsync(question, cancellationToken);

        if (!selection.IsMalformed &&
            string.Equals(
                selection.ToolName,
                AgentToolNames.SearchVectorKnowledge,
                StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(selection.Query))
        {
            var searchResult = _vectorKnowledgeTool.Search(selection.Query);
            if (searchResult.IsReliableMatch)
            {
                return new AskResponse(
                    question,
                    _vectorKnowledgeTool.Name,
                    searchResult.Answer,
                    FallbackUsed: false);
            }

            return BuildFallbackResponse(question, fallbackUsed: true);
        }

        if (!selection.IsMalformed &&
            string.Equals(
                selection.ToolName,
                AgentToolNames.GetPredefinedResponse,
                StringComparison.Ordinal))
        {
            return BuildFallbackResponse(question, fallbackUsed: false);
        }

        return BuildFallbackResponse(question, fallbackUsed: true);
    }

    private AskResponse BuildFallbackResponse(string question, bool fallbackUsed)
    {
        return new AskResponse(
            question,
            _staticResponseTool.Name,
            _staticResponseTool.GetResponse(),
            fallbackUsed);
    }
}
