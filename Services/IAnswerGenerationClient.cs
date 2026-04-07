namespace SolitaAgent.Services;

public interface IAnswerGenerationClient
{
    Task<string> GenerateAnswerAsync(
        string question,
        string toolName,
        string toolOutput,
        double? similarityScore,
        CancellationToken cancellationToken = default);
}
