using System.Globalization;
using System.Text.Json;

namespace CmosCpu.Core;

public sealed record ComputerProfileValidationResult
{
    public bool IsValid { get; init; }
    public ComputerProfile? Profile { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

public sealed record ComputerProfile
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Cpu { get; init; }
    public long ClockHz { get; init; }
    public ComputerProfileMemory? Memory { get; init; }
    public IReadOnlyList<ComputerProfileDevice>? Devices { get; init; }
}

public sealed record ComputerProfileMemory
{
    public IReadOnlyList<ComputerProfileMemorySection>? Ram { get; init; }
    public IReadOnlyList<ComputerProfileMemorySection>? Rom { get; init; }
    public ComputerProfileVectors? Vectors { get; init; }
}

public sealed record ComputerProfileMemorySection
{
    public string? Start { get; init; }
    public string? Size { get; init; }
    public string? File { get; init; }
}

public sealed record ComputerProfileVectors
{
    public string? Reset { get; init; }
    public string? Nmi { get; init; }
    public string? Irq { get; init; }
}

public sealed record ComputerProfileDevice
{
    public string? Type { get; init; }
    public string? Id { get; init; }
    public string? Start { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string? DataAddress { get; init; }
    public string? StatusAddress { get; init; }
}

public static class ComputerProfileLoader
{
    public static ComputerProfileValidationResult Load(string json)
    {
        var errors = new List<string>();
        var allRanges = new List<(string name, ushort start, ushort end)>();

        ComputerProfile? profile;
        try
        {
            profile = JsonSerializer.Deserialize<ComputerProfile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON: {ex.Message}");
            return new ComputerProfileValidationResult { IsValid = false, Errors = errors };
        }

        if (profile is null)
        {
            errors.Add("Profile is null");
            return new ComputerProfileValidationResult { IsValid = false, Errors = errors };
        }

        if (string.IsNullOrWhiteSpace(profile.Id))
            errors.Add("Profile 'id' is required");

        if (profile.Cpu != "mos6502")
            errors.Add($"Unsupported CPU: '{profile.Cpu}'. Only 'mos6502' is supported.");

        if (profile.ClockHz <= 0)
            errors.Add("'clockHz' must be greater than 0");

        if (profile.Memory is null)
        {
            errors.Add("'memory' section is required");
        }
        else
        {
            ValidateMemorySections(errors, "ram", profile.Memory.Ram);
            ValidateMemorySections(errors, "rom", profile.Memory.Rom);

            if (profile.Memory.Ram is not null)
                foreach (var s in profile.Memory.Ram)
                    if (TryParseRange(s, out var start, out var end, out var err))
                        allRanges.Add(($"RAM {s.Start}", start, end));
                    else
                        errors.Add($"RAM section '{s.Start}': {err}");

            if (profile.Memory.Rom is not null)
                foreach (var s in profile.Memory.Rom)
                    if (TryParseRange(s, out var start, out var end, out var err))
                        allRanges.Add(($"ROM {s.Start}", start, end));
                    else
                        errors.Add($"ROM section '{s.Start}': {err}");

            if (profile.Memory.Vectors is not null)
                TryParseAddress(profile.Memory.Vectors.Reset, out _, "vectors.reset");

            for (int i = 0; i < allRanges.Count; i++)
                for (int j = i + 1; j < allRanges.Count; j++)
                    if (allRanges[i].start <= allRanges[j].end && allRanges[j].start <= allRanges[i].end)
                        errors.Add($"Memory overlap: '{allRanges[i].name}' overlaps with '{allRanges[j].name}'");
        }

        if (profile.Devices is not null)
        {
            foreach (var dev in profile.Devices)
            {
                string primaryAddr = dev.Type == "keyboard" ? dev.DataAddress ?? "" : dev.Start ?? "";
                if (string.IsNullOrWhiteSpace(primaryAddr))
                {
                    errors.Add($"Device '{dev.Id}': missing address (use 'start' for display, 'dataAddress' for keyboard)");
                    continue;
                }

                if (!TryParseAddress(primaryAddr, out var devAddr, "device address"))
                {
                    errors.Add($"Device '{dev.Id}': invalid address '{primaryAddr}'");
                    continue;
                }

                bool ok = TryParseAddress(dev.DataAddress, out _, "device dataAddress");
                if (!string.IsNullOrWhiteSpace(dev.DataAddress) && !ok)
                    errors.Add($"Device '{dev.Id}': invalid dataAddress '{dev.DataAddress}'");

                ok = TryParseAddress(dev.StatusAddress, out _, "device statusAddress");
                if (!string.IsNullOrWhiteSpace(dev.StatusAddress) && !ok)
                    errors.Add($"Device '{dev.Id}': invalid statusAddress '{dev.StatusAddress}'");

                string devName = $"Device {dev.Id}";
                allRanges.Add((devName, devAddr, devAddr));

                foreach (var range in allRanges)
                {
                    if (range.name == devName) continue;
                    if (devAddr >= range.start && devAddr <= range.end)
                        errors.Add($"Device '{dev.Id}' at 0x{devAddr:X4} overlaps with {range.name}");
                }
            }
        }

        return new ComputerProfileValidationResult
        {
            IsValid = errors.Count == 0,
            Profile = profile,
            Errors = errors
        };
    }

    public static ComputerProfileValidationResult LoadFromFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            return Load(json);
        }
        catch (Exception ex)
        {
            return new ComputerProfileValidationResult
            {
                IsValid = false,
                Errors = [$"Failed to load file '{path}': {ex.Message}"]
            };
        }
    }

    public static ushort ParseHex(string value)
    {
        value = value.Trim();
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return ushort.Parse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        if (value.StartsWith("$"))
            return ushort.Parse(value[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return ushort.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    private static bool TryParseAddress(string? value, out ushort address, string label)
    {
        address = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        try
        {
            address = ParseHex(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseAddress(string? value, out ushort address, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Address is empty";
            address = 0;
            return false;
        }
        try
        {
            address = ParseHex(value);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Invalid hex address '{value}': {ex.Message}";
            address = 0;
            return false;
        }
    }

    private static bool TryParseDeviceAddress(string? value, out ushort address)
    {
        address = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        try
        {
            address = ParseHex(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseRange(ComputerProfileMemorySection section, out ushort start, out ushort end, out string? error)
    {
        start = end = 0;
        error = null;

        if (!TryParseAddress(section.Start, out start, out error))
            return false;
        if (!TryParseAddress(section.Size, out var size, out error))
            return false;

        end = (ushort)(start + size - 1);
        if (end < start)
        {
            error = $"Range overflow: start 0x{start:X4} + size 0x{size:X4} exceeds 16-bit address space";
            return false;
        }

        return true;
    }

    private static void ValidateMemorySections(List<string> errors, string type, IReadOnlyList<ComputerProfileMemorySection>? sections)
    {
        if (sections is null)
            return;

        for (int i = 0; i < sections.Count; i++)
        {
            var s = sections[i];
            if (string.IsNullOrWhiteSpace(s.Start))
                errors.Add($"{type}[{i}]: 'start' is required");
            if (string.IsNullOrWhiteSpace(s.Size))
                errors.Add($"{type}[{i}]: 'size' is required");
        }
    }
}
