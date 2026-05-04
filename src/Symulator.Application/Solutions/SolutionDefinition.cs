namespace Symulator.Application.Solutions;

public sealed class SolutionDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? SourceFilePath { get; set; }
    public string EntryPoint { get; set; } = "0x0100";
    public string LoadAddress { get; set; } = "0x0100";
    public CpuDefinition Cpu { get; set; } = new();
    public MemoryDefinition Memory { get; set; } = new();
    public List<DeviceDefinition> Devices { get; set; } = [];
    public UiDefinition Ui { get; set; } = new();

    public IReadOnlySet<string> VisibleDeviceTypes =>
        Devices.Where(d => d.Visible).Select(d => d.Type).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> AllDeviceTypes =>
        Devices.Select(d => d.Type).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static SolutionDefinition CreateFallback(string id, string source)
    {
        return new SolutionDefinition
        {
            Id = id,
            Name = id,
            Source = source,
            Devices =
            [
                new DeviceDefinition
                {
                    Id = "cpu",
                    Type = "cpu",
                    Name = "CPU",
                    Visible = true
                }
            ],
            Ui = new UiDefinition
            {
                Layout = new UiLayoutDefinition
                {
                    Left = ["program-loader"],
                    Right = ["cpu"],
                    Bottom = ["asm-tabs"]
                }
            }
        };
    }

    public bool HasDevice(string type) =>
        Devices.Any(d => string.Equals(d.Type, type, StringComparison.OrdinalIgnoreCase));

    public bool HasVisibleDevice(string type) =>
        VisibleDeviceTypes.Contains(type);

    public bool HasRuntimeDevice(string type) =>
        AllDeviceTypes.Contains(type);

    public DeviceDefinition? GetDevice(string type) =>
        Devices.FirstOrDefault(d => string.Equals(d.Type, type, StringComparison.OrdinalIgnoreCase));
}

public sealed class CpuDefinition
{
    public string Type { get; set; } = "minimal-blink-cpu";
    public int ClockHz { get; set; } = 1_000_000;
}

public sealed class MemoryDefinition
{
    public List<MemoryRegionDefinition> Ram { get; set; } =
    [
        new MemoryRegionDefinition
        {
            Start = "0x0000",
            Size = "0x0100"
        }
    ];
}

public sealed class MemoryRegionDefinition
{
    public string Start { get; set; } = "0x0000";
    public string Size { get; set; } = "0x0100";
}

public sealed class DeviceDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? BaseAddress { get; set; }
    public Dictionary<string, string> Registers { get; set; } = [];
    public Dictionary<string, string> Options { get; set; } = [];
    public bool Visible { get; set; } = true;

    public string EffectiveAddress => Address ?? BaseAddress ?? string.Empty;
}

public sealed class UiDefinition
{
    public UiLayoutDefinition Layout { get; set; } = new();
}

public sealed class UiLayoutDefinition
{
    public List<string> Left { get; set; } = [];
    public List<string> Center { get; set; } = [];
    public List<string> Right { get; set; } = [];
    public List<string> Bottom { get; set; } = [];
}

public static class AddressParser
{
    public static ushort Parse16(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var trimmed = value.Trim();

        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return Convert.ToUInt16(trimmed[2..], 16);

        if (trimmed.StartsWith('$'))
            return Convert.ToUInt16(trimmed[1..], 16);

        return Convert.ToUInt16(trimmed, 10);
    }
}
