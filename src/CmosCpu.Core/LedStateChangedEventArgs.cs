namespace CmosCpu.Core;

public class LedStateChangedEventArgs : EventArgs
{
    public bool IsOn { get; init; }
}
