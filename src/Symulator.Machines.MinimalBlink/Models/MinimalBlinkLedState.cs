namespace Symulator.Machines.MinimalBlink.Models;

public sealed class MinimalBlinkLedState
{
    public bool IsOn { get; set; }
    public byte LastValue { get; set; }
    public long ToggleCount { get; set; }

    public void Write(byte value)
    {
        bool wasOn = IsOn;
        IsOn = value != 0;
        LastValue = value;
        if (wasOn != IsOn)
            ToggleCount++;
    }

    public void Reset()
    {
        IsOn = false;
        LastValue = 0;
        ToggleCount = 0;
    }
}
