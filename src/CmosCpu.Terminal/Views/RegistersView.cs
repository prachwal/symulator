using CmosCpu.Computer;
using CmosCpu.Cpu;
using Spectre.Console;

namespace CmosCpu.Terminal.Views;

public static class RegistersView
{
    public static void Render(Mos6502Cpu cpu)
    {
        var table = new Table();
        table.AddColumn("Register");
        table.AddColumn("Hex");
        table.AddColumn("Dec");
        table.AddColumn("Bin");

        table.AddRow("A", $"0x{cpu.A:X2}", cpu.A.ToString(), Convert.ToString(cpu.A, 2).PadLeft(8, '0'));
        table.AddRow("X", $"0x{cpu.X:X2}", cpu.X.ToString(), Convert.ToString(cpu.X, 2).PadLeft(8, '0'));
        table.AddRow("Y", $"0x{cpu.Y:X2}", cpu.Y.ToString(), Convert.ToString(cpu.Y, 2).PadLeft(8, '0'));
        table.AddRow("PC", $"0x{cpu.PC:X4}", cpu.PC.ToString(), Convert.ToString(cpu.PC, 2).PadLeft(16, '0'));
        table.AddRow("SP", $"0x{cpu.SP:X2}", cpu.SP.ToString(), Convert.ToString(cpu.SP, 2).PadLeft(8, '0'));
        table.AddRow("Cycles", $"0x{cpu.CycleCount:X8}", cpu.CycleCount.ToString(), "");

        AnsiConsole.Write(table);

        var flagTable = new Table();
        flagTable.AddColumn("Flag");
        flagTable.AddColumn("Value");

        flagTable.AddRow("N (Negative)", MarkupBool(cpu.Negative));
        flagTable.AddRow("V (Overflow)", MarkupBool(cpu.Overflow));
        flagTable.AddRow("B (Break)", MarkupBool(cpu.Break));
        flagTable.AddRow("D (Decimal)", MarkupBool(cpu.Decimal));
        flagTable.AddRow("I (Int Disable)", MarkupBool(cpu.InterruptDisable));
        flagTable.AddRow("Z (Zero)", MarkupBool(cpu.Zero));
        flagTable.AddRow("C (Carry)", MarkupBool(cpu.Carry));

        AnsiConsole.Write(flagTable);
    }

    public static string FormatRegistersLine(Mos6502Cpu cpu)
    {
        return $"A=0x{cpu.A:X2} X=0x{cpu.X:X2} Y=0x{cpu.Y:X2} " +
               $"PC=0x{cpu.PC:X4} SP=0x{cpu.SP:X2} " +
               $"Cycles={cpu.CycleCount} " +
               $"NV-BDIZC={ConvertFlags(cpu)}";
    }

    private static string ConvertFlags(Mos6502Cpu cpu)
    {
        return $"{(cpu.Negative ? '1' : '0')}{(cpu.Overflow ? '1' : '0')}" +
               $"{(cpu.Break ? '1' : '0')}{(cpu.Decimal ? '1' : '0')}" +
               $"{(cpu.InterruptDisable ? '1' : '0')}{(cpu.Zero ? '1' : '0')}" +
               $"{(cpu.Carry ? '1' : '0')}";
    }

    private static string MarkupBool(bool value)
    {
        return value ? "[green]1[/]" : "[grey]0[/]";
    }
}
