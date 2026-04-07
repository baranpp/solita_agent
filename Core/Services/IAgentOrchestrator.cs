using SolitaAgent.Core.Contracts;

namespace SolitaAgent.Core.Services;

public interface IAgentOrchestrator
{
    Task<AskResponse> AskAsync(string question, CancellationToken cancellationToken = default);
}
