using CmosCpu.Core;

namespace CmosCpu.Runtime;

public sealed class DebuggerService : IDebugger
{
    private readonly HashSet<ushort> _breakpoints = new();

    public IReadOnlySet<ushort> Breakpoints => _breakpoints;

    public void AddBreakpoint(ushort address)
    {
        _breakpoints.Add(address);
    }

    public void RemoveBreakpoint(ushort address)
    {
        _breakpoints.Remove(address);
    }

    public void ClearBreakpoints()
    {
        _breakpoints.Clear();
    }

    public bool IsBreakpoint(ushort address)
    {
        return _breakpoints.Contains(address);
    }
}