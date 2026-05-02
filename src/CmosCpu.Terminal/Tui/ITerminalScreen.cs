using CmosCpu.Terminal.Session;

namespace CmosCpu.Terminal.Tui;

public interface ITerminalScreen
{
    void Run(ISimulatorSession session);
}
