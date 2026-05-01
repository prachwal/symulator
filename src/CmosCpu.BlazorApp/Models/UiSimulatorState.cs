using CmosCpu.Core;

namespace CmosCpu.BlazorApp.Models;

public class UiSimulatorState
{
    public byte A { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
    public ushort PC { get; set; }
    public ushort SP { get; set; }
    public ulong Cycles { get; set; }
    public bool Halted { get; set; }
    public string CurrentInstruction { get; set; } = "";

    public bool Zero { get; set; }
    public bool Carry { get; set; }
    public bool Negative { get; set; }
    public bool InterruptDisable { get; set; }
    public string FlagsText { get; set; } = "";

    public bool LedOn { get; set; }

    public string TimerCounter { get; set; } = "0";
    public string TimerControl { get; set; } = "0x00";
    public string TimerStatus { get; set; } = "0x00";
    public bool TimerRunning { get; set; }

    public string RtcSecond { get; set; } = "0";
    public string RtcMinute { get; set; } = "0";
    public string RtcHour { get; set; } = "0";
    public string RtcDay { get; set; } = "0";
    public string RtcMonth { get; set; } = "0";
    public string RtcYear { get; set; } = "0";

    public bool IrqPending { get; set; }
    public bool NmiPending { get; set; }
    public string ResetVector { get; set; } = "0x8000";
    public string IrqVector { get; set; } = "0x0000";
    public string NmiVector { get; set; } = "0x0000";

    public List<MemoryRowViewModel> MemoryRows { get; set; } = new();
    public List<TraceEntryViewModel> TraceEntries { get; set; } = new();
    public List<BusTransactionViewModel> BusTransactions { get; set; } = new();

    public void UpdateFromSnapshot(SimulatorSnapshot snap)
    {
        A = snap.Registers.A;
        X = snap.Registers.X;
        Y = snap.Registers.Y;
        PC = snap.Registers.PC;
        SP = (ushort)(0x0100 + snap.Registers.SP);
        Cycles = snap.CycleCount;
        Halted = snap.Registers.Halted;
        CurrentInstruction = snap.CurrentInstruction ?? "";

        Zero = snap.Registers.ZeroFlag;
        Carry = snap.Registers.CarryFlag;
        Negative = snap.Registers.NegativeFlag;
        InterruptDisable = snap.Registers.InterruptDisableFlag;
        FlagsText = snap.Registers.Flags.ToString();

        LedOn = snap.LedOn;

        if (snap.Timer != null)
        {
            TimerCounter = snap.Timer.Counter.ToString();
            TimerControl = $"0x{snap.Timer.Control:X2}";
            TimerStatus = $"0x{snap.Timer.Status:X2}";
            TimerRunning = snap.Timer.Running;
        }

        if (snap.Interrupts != null)
        {
            ResetVector = $"0x{snap.Interrupts.ResetVector:X4}";
            IrqVector = $"0x{snap.Interrupts.IrqVector:X4}";
            NmiVector = $"0x{snap.Interrupts.NmiVector:X4}";
        }

        MemoryRows.Clear();
        var memDict = new Dictionary<ushort, byte>();
        foreach (var cell in snap.MemoryWindow)
            memDict[cell.Address] = cell.Value;

        ushort addr = (ushort)(snap.Registers.PC & 0xFFF0);
        for (int row = 0; row < 16; row++)
        {
            var rowVm = new MemoryRowViewModel { Address = addr };
            for (int col = 0; col < 16; col++)
            {
                ushort currentAddr = (ushort)(addr + col);
                byte value = memDict.TryGetValue(currentAddr, out var v) ? v : (byte)0;
                rowVm.SetCell(col, value, currentAddr == snap.Registers.PC);
            }
            MemoryRows.Add(rowVm);
            addr += 16;
        }
    }
}
