using CmosCpu.Computer.Devices;

namespace Symulator.Machines.Apple1.Devices;

public sealed class Apple1PiaWiring
{
    private readonly Pia6821 _pia;
    private readonly Apple1TerminalBuffer _buffer;
    private byte _pendingKey;

    public Apple1PiaWiring(Pia6821 pia, Apple1TerminalBuffer buffer)
    {
        _pia = pia;
        _buffer = buffer;

        ushort craAddr = (ushort)(pia.StartAddress + 1);
        ushort crbAddr = (ushort)(pia.StartAddress + 3);
        ushort portAAddr = (ushort)(pia.StartAddress + 0);
        ushort portBAddr = (ushort)(pia.StartAddress + 2);

        // Initialize Port A for keyboard input (bit2=1 → data mode reads input pins)
        pia.Write(craAddr, 0x04);  // CRA bit2=1 → data mode

        // Initialize Port B for display output: bits 0-6 output, bit 7 input (display-ready status)
        pia.Write(crbAddr, 0x00);  // CRB bit2=0 → DDR
        pia.Write(portBAddr, 0x7F); // DDRB = 0x7F (bits 0-6 output, bit7 input for display-ready)
        pia.PortBInput = 0x00;     // bit7=0 → display always ready
        pia.Write(crbAddr, 0x04);  // CRB bit2=1 → data mode

        // Port B output → terminal display
        _pia.PortBOutputChanged += OnPortBOutputChanged;

        // Clear pending key when CPU reads Port A (key consumed)
        _pia.OnPortARead += ClearPendingKey;

        // Port A input = keyboard data with bit 7 (key ready)
        UpdateKeyboardInput();
    }

    public bool HasPendingKey => _pendingKey != 0;

    public void QueueKey(char key)
    {
        _pendingKey = (byte)((key & 0x7F) | 0x80);
        UpdateKeyboardInput();
        _pia.SetCa1(true);
    }

    private void OnPortBOutputChanged(byte value)
    {
        _buffer.Write(value);
    }

    private void UpdateKeyboardInput()
    {
        _pia.PortAInput = _pendingKey;
    }

    public void ClearPendingKey()
    {
        _pendingKey = 0;
        UpdateKeyboardInput();
    }
}
