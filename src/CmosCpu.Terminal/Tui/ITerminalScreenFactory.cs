namespace CmosCpu.Terminal.Tui;

public interface ITerminalScreenFactory
{
    ITerminalScreen Create(string platformId);
    bool Supports(string platformId);
}
