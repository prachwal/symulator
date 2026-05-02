using CmosCpu.Core;

using NLog;

namespace CmosCpu.Cpu;

public class CpuCore
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IBus _bus;
    private readonly CpuRegisters _regs = new();
    private bool _nmiPending;
    private bool _irqPending;

    public CpuRegisters Registers => _regs;
    public bool Halted => _regs.Halted;
    public ulong CycleCount => _regs.CycleCount;

    public CpuCore(IBus bus)
    {
        _bus = bus;
    }

    public void Reset()
    {
        _regs.A = 0;
        _regs.X = 0;
        _regs.Y = 0;
        _regs.SP = 0xFF;
        _regs.Flags = CpuFlags.InterruptDisable;
        _regs.Halted = false;
        _regs.CycleCount = 0;
        _nmiPending = false;
        _irqPending = false;

        byte lo = _bus.Read(0xFFFC);
        byte hi = _bus.Read(0xFFFD);
        _regs.PC = (ushort)((hi << 8) | lo);

        Logger.Info("CPU reset. PC = {PC:X4}", _regs.PC);
    }

    public void RequestNmi()
    {
        _nmiPending = true;
    }

    public void RequestInterrupt()
    {
        _irqPending = true;
    }

    public void Step()
    {
        if (_regs.Halted)
            return;

        if (_nmiPending)
        {
            HandleNmi();
            return;
        }

        if (_irqPending && !_regs.InterruptDisableFlag)
        {
            HandleIrq();
            return;
        }

        byte opcodeByte = Fetch();
        var opcode = (Opcode)opcodeByte;

        Logger.Trace("Cycle {Cycles}: PC={PC:X4} Opcode={Opcode:X2} {Mnemonic}",
            _regs.CycleCount, _regs.PC - 1, opcodeByte, InstructionInfo.All.TryGetValue(opcode, out var info) ? info.Mnemonic : "???");

        Execute(opcode);
    }

    private byte Fetch()
    {
        byte value = _bus.Read(_regs.PC);
        _regs.PC++;
        _regs.CycleCount++;
        return value;
    }

    private ushort FetchWord()
    {
        byte lo = Fetch();
        byte hi = Fetch();
        return (ushort)((hi << 8) | lo);
    }

    private void Push(byte value)
    {
        ushort address = (ushort)(0x0100 + _regs.SP);
        _bus.Write(address, value);
        _regs.SP--;
    }

    private byte Pop()
    {
        _regs.SP++;
        ushort address = (ushort)(0x0100 + _regs.SP);
        return _bus.Read(address);
    }

    private void HandleNmi()
    {
        _nmiPending = false;
        Push((byte)((_regs.PC >> 8) & 0xFF));
        Push((byte)(_regs.PC & 0xFF));
        Push((byte)_regs.Flags);
        _regs.InterruptDisableFlag = true;
        byte lo = _bus.Read(0xFFFE);
        byte hi = _bus.Read(0xFFFF);
        _regs.PC = (ushort)((hi << 8) | lo);
        _regs.CycleCount += 5;
        Logger.Debug("NMI handled, PC -> {PC:X4}", _regs.PC);
    }

    private void HandleIrq()
    {
        _irqPending = false;
        Push((byte)((_regs.PC >> 8) & 0xFF));
        Push((byte)(_regs.PC & 0xFF));
        Push((byte)_regs.Flags);
        _regs.InterruptDisableFlag = true;
        byte lo = _bus.Read(0xFFFA);
        byte hi = _bus.Read(0xFFFB);
        _regs.PC = (ushort)((hi << 8) | lo);
        _regs.CycleCount += 5;
        Logger.Debug("IRQ handled, PC -> {PC:X4}", _regs.PC);
    }

    private void Execute(Opcode opcode)
    {
        switch (opcode)
        {
            case Opcode.NOP:
                break;

            case Opcode.LDA_IMM:
                {
                    byte value = Fetch();
                    _regs.A = value;
                    _regs.SetZeroAndNegativeFlags(value);
                    break;
                }

            case Opcode.LDA_ABS:
                {
                    ushort addr = FetchWord();
                    byte value = _bus.Read(addr);
                    _regs.A = value;
                    _regs.SetZeroAndNegativeFlags(value);
                    break;
                }

            case Opcode.STA_ABS:
                {
                    ushort addr = FetchWord();
                    _bus.Write(addr, _regs.A);
                    break;
                }

            case Opcode.ADD_IMM:
                {
                    byte value = Fetch();
                    ushort sum = (ushort)(_regs.A + value);
                    _regs.CarryFlag = sum > 0xFF;
                    _regs.A = (byte)(sum & 0xFF);
                    _regs.SetZeroAndNegativeFlags(_regs.A);
                    break;
                }

            case Opcode.SUB_IMM:
                {
                    byte value = Fetch();
                    ushort diff = (ushort)(_regs.A - value);
                    _regs.CarryFlag = diff <= 0xFF;
                    _regs.A = (byte)(diff & 0xFF);
                    _regs.SetZeroAndNegativeFlags(_regs.A);
                    break;
                }

            case Opcode.JMP:
                {
                    ushort addr = FetchWord();
                    _regs.PC = addr;
                    break;
                }

            case Opcode.JZ:
                {
                    ushort addr = FetchWord();
                    if (_regs.ZeroFlag)
                        _regs.PC = addr;
                    break;
                }

            case Opcode.JNZ:
                {
                    ushort addr = FetchWord();
                    if (!_regs.ZeroFlag)
                        _regs.PC = addr;
                    break;
                }

            case Opcode.OUT:
                {
                    byte port = Fetch();
                    _bus.Write((ushort)(0xC000 + port), _regs.A);
                    Logger.Debug("OUT port {Port:X2} = {Value:X2}", port, _regs.A);
                    break;
                }

            case Opcode.IN:
                {
                    byte port = Fetch();
                    byte value = _bus.Read((ushort)(0xC000 + port));
                    _regs.A = value;
                    break;
                }

            case Opcode.CLI:
                {
                    _regs.InterruptDisableFlag = false;
                    break;
                }

            case Opcode.SEI:
                {
                    _regs.InterruptDisableFlag = true;
                    break;
                }

            case Opcode.PUSH_A:
                {
                    Push(_regs.A);
                    break;
                }

            case Opcode.POP_A:
                {
                    _regs.A = Pop();
                    _regs.SetZeroAndNegativeFlags(_regs.A);
                    break;
                }

            case Opcode.CALL:
                {
                    ushort addr = FetchWord();
                    ushort returnAddr = _regs.PC;
                    Push((byte)((returnAddr >> 8) & 0xFF));
                    Push((byte)(returnAddr & 0xFF));
                    _regs.PC = addr;
                    break;
                }

            case Opcode.RET:
                {
                    byte lo = Pop();
                    byte hi = Pop();
                    _regs.PC = (ushort)((hi << 8) | lo);
                    break;
                }

            case Opcode.HLT:
                {
                    _regs.Halted = true;
                    Logger.Info("CPU HALTED at cycle {Cycles}", _regs.CycleCount);
                    break;
                }

            default:
                Logger.Warn("Unknown opcode {Opcode:X2} at PC {PC:X4}", (byte)opcode, _regs.PC - 1);
                break;
        }
    }

    public void Run(int maxCycles)
    {
        ulong target = _regs.CycleCount + (ulong)maxCycles;
        while (_regs.CycleCount < target && !_regs.Halted)
            Step();
    }
}