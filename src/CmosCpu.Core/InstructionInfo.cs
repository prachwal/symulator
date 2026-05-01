using System.Collections.Generic;

namespace CmosCpu.Core;

public class InstructionInfo
{
    public Opcode Opcode { get; init; }
    public string Mnemonic { get; init; } = string.Empty;
    public string OperandFormat { get; init; } = string.Empty;
    public int ByteCount { get; init; }
    public int CycleCount { get; init; }
    public string Description { get; init; } = string.Empty;

    public static readonly Dictionary<Opcode, InstructionInfo> All = new()
    {
        [Opcode.NOP] = new() { Opcode = Opcode.NOP, Mnemonic = "NOP", ByteCount = 1, CycleCount = 1, Description = "No operation" },
        [Opcode.LDA_IMM] = new() { Opcode = Opcode.LDA_IMM, Mnemonic = "LDA", OperandFormat = "#value", ByteCount = 2, CycleCount = 2, Description = "Load A with immediate value" },
        [Opcode.LDA_ABS] = new() { Opcode = Opcode.LDA_ABS, Mnemonic = "LDA", OperandFormat = "addr", ByteCount = 3, CycleCount = 4, Description = "Load A from absolute address" },
        [Opcode.STA_ABS] = new() { Opcode = Opcode.STA_ABS, Mnemonic = "STA", OperandFormat = "addr", ByteCount = 3, CycleCount = 4, Description = "Store A to absolute address" },
        [Opcode.ADD_IMM] = new() { Opcode = Opcode.ADD_IMM, Mnemonic = "ADD", OperandFormat = "#value", ByteCount = 2, CycleCount = 2, Description = "Add immediate to A" },
        [Opcode.SUB_IMM] = new() { Opcode = Opcode.SUB_IMM, Mnemonic = "SUB", OperandFormat = "#value", ByteCount = 2, CycleCount = 2, Description = "Subtract immediate from A" },
        [Opcode.JMP] = new() { Opcode = Opcode.JMP, Mnemonic = "JMP", OperandFormat = "addr", ByteCount = 3, CycleCount = 3, Description = "Jump to address" },
        [Opcode.JZ] = new() { Opcode = Opcode.JZ, Mnemonic = "JZ", OperandFormat = "addr", ByteCount = 3, CycleCount = 2, Description = "Jump if Zero flag set" },
        [Opcode.JNZ] = new() { Opcode = Opcode.JNZ, Mnemonic = "JNZ", OperandFormat = "addr", ByteCount = 3, CycleCount = 2, Description = "Jump if Zero flag not set" },
        [Opcode.OUT] = new() { Opcode = Opcode.OUT, Mnemonic = "OUT", OperandFormat = "port", ByteCount = 2, CycleCount = 3, Description = "Write A to I/O port" },
        [Opcode.IN] = new() { Opcode = Opcode.IN, Mnemonic = "IN", OperandFormat = "port", ByteCount = 2, CycleCount = 3, Description = "Read from I/O port to A" },
        [Opcode.CLI] = new() { Opcode = Opcode.CLI, Mnemonic = "CLI", ByteCount = 1, CycleCount = 1, Description = "Clear Interrupt Disable flag" },
        [Opcode.SEI] = new() { Opcode = Opcode.SEI, Mnemonic = "SEI", ByteCount = 1, CycleCount = 1, Description = "Set Interrupt Disable flag" },
        [Opcode.PUSH_A] = new() { Opcode = Opcode.PUSH_A, Mnemonic = "PUSH_A", ByteCount = 1, CycleCount = 2, Description = "Push A onto stack" },
        [Opcode.POP_A] = new() { Opcode = Opcode.POP_A, Mnemonic = "POP_A", ByteCount = 1, CycleCount = 2, Description = "Pop A from stack" },
        [Opcode.CALL] = new() { Opcode = Opcode.CALL, Mnemonic = "CALL", OperandFormat = "addr", ByteCount = 3, CycleCount = 4, Description = "Call subroutine" },
        [Opcode.RET] = new() { Opcode = Opcode.RET, Mnemonic = "RET", ByteCount = 1, CycleCount = 4, Description = "Return from subroutine" },
        [Opcode.HLT] = new() { Opcode = Opcode.HLT, Mnemonic = "HLT", ByteCount = 1, CycleCount = 1, Description = "Halt CPU" },
    };
}
