using SolitaAgent.Core.Contracts;
using SolitaAgent.Core.Exceptions;

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
        ToolSelectionResult selection;
        try
        {
            selection = await _toolSelectionClient.SelectToolAsync(question, cancellationToken);
        }
        catch (LlmProviderException)
        {
            return BuildLocalOnlyResponse(question);
        }

        var toolResult = ExecuteSelectedTool(selection);

        return await GenerateAnswerSafely(question, toolResult, cancellationToken);
    }

    private ToolExecutionResult ExecuteSelectedTool(ToolSelectionResult selection)
    {
        if (IsVectorSearchSelected(selection))
        {
            return ExecuteVectorSearch(selection.Query!);
        }

        var isIntentional = IsStaticResponseSelected(selection);
        return ExecuteStaticResponse(isReliable: isIntentional);
    }

    private ToolExecutionResult ExecuteVectorSearch(string query)
    {
        var result = _vectorKnowledgeTool.Search(query);

        return new ToolExecutionResult(
            _vectorKnowledgeTool.Name,
            result.Answer,
            result.Score,
            result.IsReliableMatch);
    }

    private ToolExecutionResult ExecuteStaticResponse(bool isReliable)
    {
        return new ToolExecutionResult(
            _staticResponseTool.Name,
            _staticResponseTool.GetResponse(),
            SimilarityScore: null,
            IsReliableResult: isReliable);
    }

    private async Task<AskResponse> GenerateAnswerSafely(
        string question,
        ToolExecutionResult toolResult,
        CancellationToken cancellationToken)
    {
        try
        {
            var answer = await _answerGenerationClient.GenerateAnswerAsync(
                question,
                toolResult.ToolName,
                toolResult.Output,
                toolResult.SimilarityScore,
                cancellationToken);

            return new AskResponse(
                question,
                toolResult.ToolName,
                answer,
                FallbackUsed: !toolResult.IsReliableResult,
                LlmUnavailable: false);
        }
        catch (LlmProviderException)
        {
            return new AskResponse(
                question,
                toolResult.ToolName,
                toolResult.Output,
                FallbackUsed: true,
                LlmUnavailable: true);
        }
    }

    private AskResponse BuildLocalOnlyResponse(string question)
    {
        var searchResult = _vectorKnowledgeTool.Search(question);

        if (searchResult.IsReliableMatch)
        {
            return new AskResponse(
                question,
                _vectorKnowledgeTool.Name,
                searchResult.Answer,
                FallbackUsed: true,
                LlmUnavailable: true);
        }

        return new AskResponse(
            question,
            _staticResponseTool.Name,
            _staticResponseTool.GetResponse(),
            FallbackUsed: true,
            LlmUnavailable: true);
    }

    private static bool IsVectorSearchSelected(ToolSelectionResult selection)
    {
        return !selection.IsMalformed
            && string.Equals(
                selection.ToolName,
                AgentToolNames.SearchVectorKnowledge,
                StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(selection.Query);
    }

    private static bool IsStaticResponseSelected(ToolSelectionResult selection)
    {
        return !selection.IsMalformed
            && string.Equals(
                selection.ToolName,
                AgentToolNames.GetPredefinedResponse,
                StringComparison.Ordinal);
    }

    private sealed record ToolExecutionResult(
        string ToolName,
        string Output,
        double? SimilarityScore,
        bool IsReliableResult);
}
