namespace CmosCpu.Core;

public interface IDebugger
{
    IReadOnlySet<ushort> Breakpoints { get; }
    void AddBreakpoint(ushort address);
    void RemoveBreakpoint(ushort address);
    void ClearBreakpoints();
    bool IsBreakpoint(ushort address);
}