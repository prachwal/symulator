using Symulator.Machines.MinimalBlink.Memory;

namespace Symulator.Machines.MinimalBlink.Cpu;

public sealed class MinimalBlinkCpu
{
    private readonly MinimalBlinkMemory _memory;

    public ushort PC { get; set; }
    public byte A { get; set; }
    public bool Z { get; set; }
    public bool Halted { get; set; }
    public ulong CycleCount { get; set; }
    public string? LastError { get; set; }

    public MinimalBlinkCpu(MinimalBlinkMemory memory)
    {
        _memory = memory;
    }

    public void Reset()
    {
        A = 0;
        Z = false;
        Halted = false;
        CycleCount = 0;
        PC = 0;
        LastError = null;
    }

    public int Step()
    {
        if (Halted)
            return 0;

        byte opcode = _memory.ReadByte(PC);
        PC++;

        switch (opcode)
        {
            case 0x00: // NOP
                CycleCount += 1;
                return 1;

            case 0x01: // LDA_IMM byte
            {
                byte val = _memory.ReadByte((ushort)(PC));
                PC++;
                A = val;
                Z = val == 0;
                CycleCount += 2;
                return 2;
            }

            case 0x02: // STA_ABS lo hi
            {
                byte lo = _memory.ReadByte((ushort)(PC));
                byte hi = _memory.ReadByte((ushort)(PC + 1));
                PC += 2;
                ushort addr = (ushort)((hi << 8) | lo);
                _memory.WriteByte(addr, A);
                CycleCount += 3;
                return 3;
            }

            case 0x03: // JMP_ABS lo hi
            {
                byte lo = _memory.ReadByte((ushort)(PC));
                byte hi = _memory.ReadByte((ushort)(PC + 1));
                PC = (ushort)((hi << 8) | lo);
                CycleCount += 2;
                return 2;
            }

            case 0x04: // XOR_IMM byte
            {
                byte val = _memory.ReadByte((ushort)(PC));
                PC++;
                A ^= val;
                Z = A == 0;
                CycleCount += 2;
                return 2;
            }

            case 0x05: // DEC_MEM lo hi
            {
                byte lo = _memory.ReadByte((ushort)(PC));
                byte hi = _memory.ReadByte((ushort)(PC + 1));
                PC += 2;
                ushort addr = (ushort)((hi << 8) | lo);
                byte cur = _memory.ReadByte(addr);
                byte dec = (byte)(cur - 1);
                _memory.WriteByte(addr, dec);
                Z = dec == 0;
                CycleCount += 4;
                return 4;
            }

            case 0x06: // JNZ_ABS lo hi
            {
                byte lo = _memory.ReadByte((ushort)(PC));
                byte hi = _memory.ReadByte((ushort)(PC + 1));
                PC += 2;
                if (!Z)
                    PC = (ushort)((hi << 8) | lo);
                CycleCount += 2;
                return 2;
            }

            case 0x07: // HLT
                Halted = true;
                CycleCount += 1;
                return 1;

            case 0x08: // LDA_ABS lo hi
            {
                byte lo = _memory.ReadByte((ushort)(PC));
                byte hi = _memory.ReadByte((ushort)(PC + 1));
                PC += 2;
                ushort addr = (ushort)((hi << 8) | lo);
                A = _memory.ReadByte(addr);
                Z = A == 0;
                CycleCount += 3;
                return 3;
            }

            case 0x09: // STA_ZP byte
            {
                byte addr = _memory.ReadByte((ushort)(PC));
                PC++;
                _memory.WriteByte(addr, A);
                CycleCount += 2;
                return 2;
            }

            case 0x0A: // LDA_ZP byte
            {
                byte addr = _memory.ReadByte((ushort)(PC));
                PC++;
                A = _memory.ReadByte(addr);
                Z = A == 0;
                CycleCount += 2;
                return 2;
            }

            default:
                Halted = true;
                LastError = $"Unknown opcode: 0x{opcode:X2}";
                CycleCount += 1;
                return 0;
        }
    }
}
