namespace SolitaAgent.Services;

public interface IGeminiAgentClient
{
    Task<GeminiToolSelection> SelectToolAsync(
        string question,
        CancellationToken cancellationToken = default);
}
