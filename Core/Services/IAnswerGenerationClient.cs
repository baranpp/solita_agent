using SolitaAgent.Core.Contracts;

namespace SolitaAgent.Core.Services;

public interface IAnswerGenerationClient
{
    Task<string> GenerateAnswerAsync(
        AnswerGenerationRequest request,
        CancellationToken cancellationToken = default);
}
