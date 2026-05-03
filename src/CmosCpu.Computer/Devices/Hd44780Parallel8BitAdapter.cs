using NLog;

namespace CmosCpu.Computer.Devices;

/// <summary>8-bit parallel adapter: RS/RW/E/Data, executes on falling edge of E.</summary>
public sealed class Hd44780Parallel8BitAdapter
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Hd44780Lcd _lcd;
    private Hd44780Pins _lastPins;
    private byte _outputData;

    public Hd44780Parallel8BitAdapter(Hd44780Lcd lcd)
    {
        _lcd = lcd;
    }

    public byte OutputData => _outputData;

    public void WritePins(Hd44780Pins pins)
    {
        bool fallingEdge = _lastPins.E && !pins.E;

        if (fallingEdge)
        {
            if (!pins.Rw && !pins.Rs)
            {
                Logger.Trace("HD44780 8bit instruction cmd=0x{Value:X2}", pins.Data);
                _lcd.ExecuteInstruction(pins.Data);
            }
            else if (!pins.Rw && pins.Rs)
            {
                Logger.Trace("HD44780 8bit data write value=0x{Value:X2}", pins.Data);
                _lcd.WriteData(pins.Data);
            }
            else if (pins.Rw && !pins.Rs)
            {
                _outputData = _lcd.ReadStatus();
                Logger.Trace("HD44780 8bit status read value=0x{Value:X2}", _outputData);
            }
            else
            {
                _outputData = _lcd.ReadData();
                Logger.Trace("HD44780 8bit data read value=0x{Value:X2}", _outputData);
            }
        }

        _lastPins = pins;
    }
}
