namespace SolitaAgent.Tools;

public interface IStaticResponseTool
{
    string Name { get; }

    string GetResponse();
}
