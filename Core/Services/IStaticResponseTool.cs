namespace SolitaAgent.Core.Services;

public interface IStaticResponseTool
{
    string Name { get; }

    string GetResponse();
}
