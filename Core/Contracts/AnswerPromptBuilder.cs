namespace SolitaAgent.Core.Contracts;

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
