// Comments are here to help the reviewer navigate the code.

namespace SolitaAgent.Core.Contracts;

// Extracted from both LLM clients to avoid duplication.
// Lives in Core because it only formats a Core record (AnswerGenerationRequest) — no
// infrastructure dependencies.
public static class AnswerPromptBuilder
{
    public static string Build(AnswerGenerationRequest request)
    {
        var scoreInfo = request.SimilarityScore.HasValue
            ? $"\nSimilarity score: {request.SimilarityScore.Value:F2}"
            : string.Empty;

        return $"""
            User question: {request.Question}
            Tool used: {request.ToolName}
            Tool output: {request.ToolOutput}{scoreInfo}
            """;
    }
}
