using SolitaAgent.Core.Contracts;
using SolitaAgent.Core.Services;

namespace SolitaAgent.Infrastructure.Tools;

public sealed class StaticResponseTool : IStaticResponseTool
{
    private const string ResponseText =
        "I could not ground that question in the local knowledge base, so I am returning the predefined fallback response.";

    public string Name => AgentToolNames.GetPredefinedResponse;

    public string GetResponse() => ResponseText;
}
