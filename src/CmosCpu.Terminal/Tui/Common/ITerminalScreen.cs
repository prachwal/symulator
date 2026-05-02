using CmosCpu.Terminal.Session;

namespace CmosCpu.Terminal.Tui.Common;

public interface ITerminalScreen
{
    void Run(ISimulatorSession session);
}
