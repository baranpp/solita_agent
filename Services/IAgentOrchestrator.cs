using SolitaAgent.Contracts;

namespace SolitaAgent.Services;

public interface IAgentOrchestrator
{
    Task<AskResponse> AskAsync(string question, CancellationToken cancellationToken = default);
}
