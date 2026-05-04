namespace CmosCpu.Cpu.Tests.Reference;

public enum ReferenceTestState
{
    Continue,
    Success,
    Failed
}

public sealed record ReferenceTestResult(
    bool RomMissing,
    bool Success,
    int StepsExecuted,
    ushort PC,
    byte A,
    byte X,
    byte Y,
    byte SP,
    string? Error
);

public static class Reference6502TestRunner
{
    public static ReferenceTestResult Run(
        string romPath,
        ushort loadAddress,
        ushort resetVector,
        int maxSteps,
        Func<Ram64K, Mos6502Cpu, ReferenceTestState> probe)
    {
        if (!File.Exists(romPath))
            return new ReferenceTestResult(
                RomMissing: true,
                Success: false,
                StepsExecuted: 0,
                PC: 0,
                A: 0,
                X: 0,
                Y: 0,
                SP: 0,
                Error: $"ROM not found: {romPath}"
            );

        var ram = new Ram64K();
        byte[] rom = File.ReadAllBytes(romPath);
        ram.LoadBytes(loadAddress, rom);

        ram.WriteByte(0xFFFC, (byte)(resetVector & 0xFF));
        ram.WriteByte(0xFFFD, (byte)((resetVector >> 8) & 0xFF));

        var cpu = new Mos6502Cpu(ram);
        cpu.Reset();

        for (int i = 0; i < maxSteps; i++)
        {
            cpu.Step();
            var state = probe(ram, cpu);
            if (state == ReferenceTestState.Success)
            {
                return new ReferenceTestResult(
                    RomMissing: false,
                    Success: true,
                    StepsExecuted: i + 1,
                    PC: cpu.PC,
                    A: cpu.A,
                    X: cpu.X,
                    Y: cpu.Y,
                    SP: cpu.SP,
                    Error: null
                );
            }
            if (state == ReferenceTestState.Failed)
            {
                return new ReferenceTestResult(
                    RomMissing: false,
                    Success: false,
                    StepsExecuted: i + 1,
                    PC: cpu.PC,
                    A: cpu.A,
                    X: cpu.X,
                    Y: cpu.Y,
                    SP: cpu.SP,
                    Error: $"Test failed at step {i + 1}, PC=0x{cpu.PC:X4}"
                );
            }
        }

        return new ReferenceTestResult(
            RomMissing: false,
            Success: false,
            StepsExecuted: maxSteps,
            PC: cpu.PC,
            A: cpu.A,
            X: cpu.X,
            Y: cpu.Y,
            SP: cpu.SP,
            Error: $"Timeout after {maxSteps} steps, PC=0x{cpu.PC:X4}"
        );
    }

    public static string DefaultRomPath(string romName)
    {
        var assemblyDir = Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location);
        return Path.Combine(
            assemblyDir!,
            "..", "..", "..",
            "Reference", "roms", romName);
    }
}
