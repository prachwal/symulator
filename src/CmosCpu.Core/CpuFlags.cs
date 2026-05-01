using System;

namespace CmosCpu.Core;

[Flags]
public enum CpuFlags : byte
{
    None = 0,
    Zero = 1 << 0,
    Carry = 1 << 1,
    Negative = 1 << 2,
    InterruptDisable = 1 << 3,
}
