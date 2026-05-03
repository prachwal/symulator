using NLog;

namespace CmosCpu.Computer.Devices.Serial;

public sealed class UartDevice
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const byte StatusRxReady = 0x01;
    public const byte StatusTxReady = 0x02;
    public const byte StatusTxEmpty = 0x04;
    public const byte StatusOverrun = 0x08;

    private readonly Queue<byte> _rxFifo = new();
    private readonly Queue<byte> _txFifo = new();
    private readonly int _rxCapacity;
    private readonly int _txCapacity;

    private byte _control = 0x03;
    private byte _status;

    public UartDevice(int rxCapacity = 256, int txCapacity = 256)
    {
        _rxCapacity = rxCapacity;
        _txCapacity = txCapacity;
        Reset();
    }

    public event EventHandler<SerialByteEventArgs>? ByteTransmitted;

    public void Reset()
    {
        _rxFifo.Clear();
        _txFifo.Clear();
        _control = 0x03;
        _status = StatusTxReady | StatusTxEmpty;
        Logger.Debug("UART Reset");
    }

    public byte ReadData()
    {
        if (_rxFifo.Count == 0)
        {
            Logger.Debug("UART RX read empty");
            return 0x00;
        }

        byte result = _rxFifo.Dequeue();
        UpdateStatus();
        Logger.Debug("UART RX read byte=0x{Byte:X2} char='{Char}'", result, ToPrintable(result));
        return result;
    }

    public void WriteData(byte value)
    {
        if (!TxEnabled)
        {
            Logger.Debug("UART TX disabled, ignoring byte=0x{Value:X2}", value);
            return;
        }

        if (_txFifo.Count >= _txCapacity)
        {
            Logger.Warn("UART TX FIFO full, dropping byte=0x{Value:X2}", value);
            return;
        }

        _txFifo.Enqueue(value);
        Logger.Debug("UART TX byte=0x{Byte:X2} char='{Char}'", value, ToPrintable(value));
        FlushTransmit();
        UpdateStatus();
    }

    public byte ReadStatus()
    {
        UpdateStatus();
        Logger.Debug("UART status read value=0x{Value:X2} rxReady={Rx} txReady={Tx} txEmpty={TxE}",
            _status,
            (_status & StatusRxReady) != 0,
            (_status & StatusTxReady) != 0,
            (_status & StatusTxEmpty) != 0);
        return _status;
    }

    public byte ReadControl() => _control;

    public void WriteControl(byte value)
    {
        _control = value;
        if ((value & 0x40) != 0) _rxFifo.Clear();
        if ((value & 0x80) != 0) _txFifo.Clear();
        UpdateStatus();
        Logger.Debug("UART control write value=0x{Value:X2} rxEnabled={Rx} txEnabled={Tx}",
            value, RxEnabled, TxEnabled);
    }

    public void ReceiveFromTerminal(byte value)
    {
        if (!RxEnabled)
        {
            Logger.Debug("UART RX disabled, ignoring byte=0x{Value:X2}", value);
            return;
        }

        if (_rxFifo.Count >= _rxCapacity)
        {
            _status |= StatusOverrun;
            Logger.Warn("UART RX overrun, dropping byte=0x{Value:X2}", value);
            return;
        }

        _rxFifo.Enqueue(value);
        Logger.Debug("UART RX receive byte=0x{Byte:X2} char='{Char}' rxCount={Count}",
            value, ToPrintable(value), _rxFifo.Count);
        UpdateStatus();
    }

    public UartSnapshot GetSnapshot() => new()
    {
        Status = _status,
        Control = _control,
        RxCount = _rxFifo.Count,
        TxCount = _txFifo.Count,
        RxEnabled = RxEnabled,
        TxEnabled = TxEnabled,
    };

    private bool RxEnabled => (_control & 0x01) != 0;
    private bool TxEnabled => (_control & 0x02) != 0;

    private void FlushTransmit()
    {
        while (_txFifo.Count > 0)
        {
            byte val = _txFifo.Dequeue();
            ByteTransmitted?.Invoke(this, new SerialByteEventArgs(val));
        }
    }

    private void UpdateStatus()
    {
        byte s = 0;
        if (_rxFifo.Count > 0) s |= StatusRxReady;
        if (_txFifo.Count < _txCapacity) s |= StatusTxReady;
        if (_txFifo.Count == 0) s |= StatusTxEmpty;
        if ((_status & StatusOverrun) != 0) s |= StatusOverrun;
        _status = s;
    }

    private static string ToPrintable(byte b) =>
        b >= 0x20 && b <= 0x7E ? ((char)b).ToString()
        : b == 0x0D ? "CR"
        : b == 0x0A ? "LF"
        : b == 0x09 ? "TAB"
        : $"0x{b:X2}";
}
