namespace SolitaAgent.Services;

public interface IToolSelectionClient
{
    Task<ToolSelectionResult> SelectToolAsync(
        string question,
        CancellationToken cancellationToken = default);
}
