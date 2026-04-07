namespace SolitaAgent.Contracts;

public sealed record AskResponse(
    string Question,
    string SelectedTool,
    string Answer,
    bool FallbackUsed);
