using NLog;

namespace CmosCpu.Computer.Devices;

/// <summary>4-bit parallel adapter: nibble assembly on falling E edge.</summary>
public sealed class Hd44780Parallel4BitAdapter
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Hd44780Lcd _lcd;
    private Hd44780Pins _lastPins;
    private byte _outputData;
    private bool _hasHighNibble;
    private byte _highNibble;

    public Hd44780Parallel4BitAdapter(Hd44780Lcd lcd)
    {
        _lcd = lcd;
    }

    public byte OutputData => _outputData;

    public void WritePins(Hd44780Pins pins)
    {
        bool fallingEdge = _lastPins.E && !pins.E;
        if (!fallingEdge)
        {
            _lastPins = pins;
            return;
        }

        byte nibble = (byte)(pins.Data & 0xF0); // D4-D7 in upper 4 bits
        bool isFirstNibble = !_hasHighNibble;

        if (isFirstNibble)
        {
            // During 4-bit init, the first writes use single-nibble "commands"
            // before the 4-bit mode is formally set. Handle pragmatically:
            // If this is the first nibble of a known init sequence pattern,
            // assemble and execute immediately.
            _highNibble = nibble;
            _hasHighNibble = true;
            Logger.Trace("HD44780 4-bit high nibble: 0x{Nibble:X2} rs={Rs} rw={Rw}", nibble >> 4, pins.Rs, pins.Rw);
        }
        else
        {
            byte lowNibble = (byte)(nibble >> 4); // D4-D7 → lower 4 bits
            byte value = (byte)(_highNibble | lowNibble);
            _hasHighNibble = false;

            Logger.Trace("HD44780 4-bit assembled: high=0x{H:X2} low=0x{L:X2} -> value=0x{Value:X2} rs={Rs} rw={Rw}",
                _highNibble >> 4, lowNibble, value, pins.Rs, pins.Rw);

            if (!pins.Rw && !pins.Rs)
                _lcd.ExecuteInstruction(value);
            else if (!pins.Rw && pins.Rs)
                _lcd.WriteData(value);
            else if (pins.Rw && !pins.Rs)
                _outputData = _lcd.ReadStatus();
            else
                _outputData = _lcd.ReadData();
        }

        _lastPins = pins;
    }

    public void ResetNibbleState()
    {
        _hasHighNibble = false;
        _highNibble = 0;
    }
}
