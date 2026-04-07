namespace SolitaAgent.Infrastructure.Tools;

public interface IStaticResponseTool
{
    string Name { get; }

    string GetResponse();
}
