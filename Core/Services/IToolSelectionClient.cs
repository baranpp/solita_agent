namespace SolitaAgent.Core.Services;

public interface IToolSelectionClient
{
    Task<ToolSelectionResult> SelectToolAsync(
        string question,
        CancellationToken cancellationToken = default);
}
