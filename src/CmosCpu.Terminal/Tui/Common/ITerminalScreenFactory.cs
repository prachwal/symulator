namespace CmosCpu.Terminal.Tui.Common;

public interface ITerminalScreenFactory
{
    ITerminalScreen Create(string platformId);
    bool Supports(string platformId);
}
