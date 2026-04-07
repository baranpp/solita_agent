using SolitaAgent.Core.Contracts;

namespace SolitaAgent.Core.Services;

public sealed class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IToolSelectionClient _toolSelectionClient;
    private readonly IAnswerGenerationClient _answerGenerationClient;
    private readonly IVectorKnowledgeTool _vectorKnowledgeTool;
    private readonly IStaticResponseTool _staticResponseTool;

    public AgentOrchestrator(
        IToolSelectionClient toolSelectionClient,
        IAnswerGenerationClient answerGenerationClient,
        IVectorKnowledgeTool vectorKnowledgeTool,
        IStaticResponseTool staticResponseTool)
    {
        _toolSelectionClient = toolSelectionClient;
        _answerGenerationClient = answerGenerationClient;
        _vectorKnowledgeTool = vectorKnowledgeTool;
        _staticResponseTool = staticResponseTool;
    }

    public async Task<AskResponse> AskAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        var selection = await _toolSelectionClient.SelectToolAsync(question, cancellationToken);

        if (!selection.IsMalformed &&
            string.Equals(
                selection.ToolName,
                AgentToolNames.SearchVectorKnowledge,
                StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(selection.Query))
        {
            var searchResult = _vectorKnowledgeTool.Search(selection.Query);

            var answer = await _answerGenerationClient.GenerateAnswerAsync(
                question,
                _vectorKnowledgeTool.Name,
                searchResult.Answer,
                searchResult.Score,
                cancellationToken);

            return new AskResponse(
                question,
                _vectorKnowledgeTool.Name,
                answer,
                FallbackUsed: !searchResult.IsReliableMatch);
        }

        if (!selection.IsMalformed &&
            string.Equals(
                selection.ToolName,
                AgentToolNames.GetPredefinedResponse,
                StringComparison.Ordinal))
        {
            return await BuildFallbackResponseAsync(question, fallbackUsed: false, cancellationToken);
        }

        return await BuildFallbackResponseAsync(question, fallbackUsed: true, cancellationToken);
    }

    private async Task<AskResponse> BuildFallbackResponseAsync(
        string question,
        bool fallbackUsed,
        CancellationToken cancellationToken)
    {
        var toolOutput = _staticResponseTool.GetResponse();

        var answer = await _answerGenerationClient.GenerateAnswerAsync(
            question,
            _staticResponseTool.Name,
            toolOutput,
            similarityScore: null,
            cancellationToken);

        return new AskResponse(
            question,
            _staticResponseTool.Name,
            answer,
            fallbackUsed);
    }
}
