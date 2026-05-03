namespace Symulator.Machines.Kim1.Devices;

public sealed class Kim1LedDisplayState
{
    private string _digits = "------";
    private long _version;

    public string Digits => _digits;
    public long Version => _version;

    public void Update(string digits)
    {
        if (_digits != digits)
        {
            _digits = digits;
            _version++;
        }
    }
}
