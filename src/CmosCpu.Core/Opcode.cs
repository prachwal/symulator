using System;

namespace CmosCpu.Core;

public enum Opcode : byte
{
    NOP = 0x00,
    LDA_IMM = 0x01,
    LDA_ABS = 0x02,
    STA_ABS = 0x03,
    ADD_IMM = 0x04,
    SUB_IMM = 0x05,
    JMP = 0x06,
    JZ = 0x07,
    JNZ = 0x08,
    OUT = 0x09,
    IN = 0x0A,
    CLI = 0x0B,
    SEI = 0x0C,
    PUSH_A = 0x0D,
    POP_A = 0x0E,
    CALL = 0x0F,
    RET = 0x10,
    HLT = 0x11,
}