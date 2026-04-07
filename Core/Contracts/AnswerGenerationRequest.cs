namespace SolitaAgent.Core.Contracts;

public sealed record AnswerGenerationRequest(
    string Question,
    string ToolName,
    string ToolOutput,
    double? SimilarityScore);
