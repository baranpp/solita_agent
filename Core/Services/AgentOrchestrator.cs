// NOTE: Comments in this codebase are added solely to help the reviewer
// read and navigate the code. They would not be present in a production codebase.

using SolitaAgent.Core.Contracts;
using SolitaAgent.Core.Exceptions;

namespace SolitaAgent.Core.Services;

// Core orchestrator — the main use case of the application.
// All dependencies are interfaces (Dependency Inversion Principle).
// This class lives in Core and has zero knowledge of Infrastructure.
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

    // Main flow: validate → LLM selects a tool → execute tool → LLM generates answer.
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

    // If answer generation fails, falls back to the raw tool output instead of throwing.
    private async Task<AskResponse> GenerateAnswerSafely(
        string question,
        ToolExecutionResult toolResult,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new AnswerGenerationRequest(
                question,
                toolResult.ToolName,
                toolResult.Output,
                toolResult.SimilarityScore);

            var answer = await _answerGenerationClient.GenerateAnswerAsync(
                request,
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

    // Graceful degradation: when the LLM is completely unavailable, try vector search
    // locally and fall back to the static response if that also misses.
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
