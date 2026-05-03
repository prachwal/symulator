using CmosCpu.Computer.Abstractions;

namespace CmosCpu.Computer.Devices;

public sealed class Pia6821 : IMemoryMappedDevice, IInterruptSource
{
    private byte _portAData;
    private byte _portADdr;
    private byte _portAInput;
    private byte _cra;

    private byte _portBData;
    private byte _portBDdr;
    private byte _portBInput;
    private byte _crb;

    public ushort StartAddress { get; }
    public ushort EndAddress { get; }

    bool IInterruptSource.IrqPending => IrqActive;
    public bool IrqActive => ((_cra & 0x82) == 0x82)
                          || ((_crb & 0x82) == 0x82);

    public byte PortAOutput => (byte)(_portAData & _portADdr);
    public byte PortBOutput => (byte)(_portBData & _portBDdr);
    public byte PortAInput { get => _portAInput; set => _portAInput = value; }
    public byte PortBInput { get => _portBInput; set => _portBInput = value; }
    public Action? OnPortARead { get; set; }

    public event Action<byte>? PortAOutputChanged;
    public event Action<byte>? PortBOutputChanged;

    public Pia6821(ushort startAddress = 0xD010, ushort endAddress = 0xD013)
    {
        StartAddress = startAddress;
        EndAddress = endAddress;
    }

    public void Reset()
    {
        _portAData = 0;
        _portADdr = 0;
        _portAInput = 0;
        _cra = 0;
        _portBData = 0;
        _portBDdr = 0;
        _portBInput = 0;
        _crb = 0;
    }

    public byte Read(ushort address)
    {
        int offset = address - StartAddress;
        offset &= 0x03;

        switch (offset)
        {
            case 0: return ReadDataA();
            case 1: return _cra;
            case 2: return ReadDataB();
            case 3: return _crb;
            default: return 0;
        }
    }

    public void Write(ushort address, byte value)
    {
        int offset = address - StartAddress;
        offset &= 0x03;

        switch (offset)
        {
            case 0: WriteDataA(value); break;
            case 1: _cra = (byte)((value & 0x3F) | (_cra & 0xC0)); break;
            case 2: WriteDataB(value); break;
            case 3: _crb = (byte)((value & 0x3F) | (_crb & 0xC0)); break;
        }
    }

    private byte ReadDataA()
    {
        if ((_cra & 0x04) != 0)
        {
            _cra &= 0x7F;
            byte result = (byte)((_portAData & _portADdr) | (_portAInput & ~_portADdr));
            OnPortARead?.Invoke();
            return result;
        }
        OnPortARead?.Invoke();
        return _portADdr;
    }

    private void WriteDataA(byte value)
    {
        if ((_cra & 0x04) != 0)
        {
            if (_portAData != value)
            {
                _portAData = value;
                PortAOutputChanged?.Invoke(PortAOutput);
            }
        }
        else
        {
            _portADdr = value;
        }
    }

    private byte ReadDataB()
    {
        if ((_crb & 0x04) != 0)
        {
            _crb &= 0x7F;
            return (byte)((_portBData & _portBDdr) | (_portBInput & ~_portBDdr));
        }
        return _portBDdr;
    }

    private void WriteDataB(byte value)
    {
        if ((_crb & 0x04) != 0)
        {
            _portBData = value;
            PortBOutputChanged?.Invoke(PortBOutput);
        }
        else
        {
            _portBDdr = value;
        }
    }

    public void SetCa1(bool level)
    {
        if (level)
        {
            _cra |= 0x80;
        }
    }

    public void SetCb1(bool level)
    {
        if (level)
        {
            _crb |= 0x80;
        }
    }
}
