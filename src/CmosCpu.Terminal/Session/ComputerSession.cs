using CmosCpu.Assembler;
using CmosCpu.Computer;
using CmosCpu.Core;
using NLog;

namespace CmosCpu.Terminal.Session;

public sealed class ComputerSession : ISimulatorSession
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly ComputerRunController _controller = new();
    private readonly SimpleAssembler _assembler = new();
    private string _currentProfileJson = "";

    public ComputerMachine? Machine => _controller.Machine;
    public bool IsLoaded => _controller.Machine is not null;
    public bool IsRunning => _controller.IsRunning;
    public long TotalInstructionsExecuted => _controller.TotalInstructionsExecuted;
    public string? LastError => _controller.LastError;

    public ComputerRunController Controller => _controller;

    public bool LoadProfile(string jsonOrFilePath)
    {
        if (jsonOrFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(jsonOrFilePath))
                return LoadProfileFromFile(jsonOrFilePath);
        }

        var result = ComputerProfileLoader.Load(jsonOrFilePath);
        if (!result.IsValid || result.Profile is null)
        {
            Logger.Error("Failed to load profile: {Errors}",
                string.Join("; ", result.Errors));
            return false;
        }

        var machine = new ComputerMachine(result.Profile);
        _controller.SetMachine(machine);
        _currentProfileJson = jsonOrFilePath;
        TryLoadProfileRoms(machine, result.Profile);
        machine.Reset();
        Logger.Info("Loaded profile: {Profile}", result.Profile.Name);
        return true;
    }

    public bool LoadProfileFromFile(string path)
    {
        if (!File.Exists(path))
        {
            Logger.Error("Profile file not found: {Path}", path);
            return false;
        }

        var result = ComputerProfileLoader.LoadFromFile(path);
        if (!result.IsValid || result.Profile is null)
        {
            Logger.Error("Failed to load profile from {Path}: {Errors}",
                path, string.Join("; ", result.Errors));
            return false;
        }

        var machine = new ComputerMachine(result.Profile);
        _controller.SetMachine(machine);
        _currentProfileJson = path;
        Logger.Info("Loaded profile from file: {Path} -> {Profile}", path, result.Profile.Name);
        TryLoadProfileRoms(machine, result.Profile, path);
        machine.Reset();
        return true;
    }

    public void Reset()
    {
        _controller.Machine?.Reset();
        Logger.Info("Machine reset");
    }

    public int Step()
    {
        if (_controller.Machine is null) return 0;
        return _controller.Machine.Step();
    }

    public Task StartAsync(CancellationToken? externalToken = null)
    {
        if (_controller.Machine is null) return Task.CompletedTask;
        return _controller.StartAsync(externalToken);
    }

    public void Stop()
    {
        _controller.Stop();
    }

    public void SetSpeed(int instructionsPerBatch, int renderDelayMs)
    {
        _controller.InstructionsPerBatch = instructionsPerBatch;
        _controller.RenderDelayMs = renderDelayMs;
    }

    public (int batch, int delay) GetSpeed()
    {
        return (_controller.InstructionsPerBatch, _controller.RenderDelayMs);
    }

    public byte[] ReadMemory(ushort start, int length)
    {
        if (_controller.Machine is null)
            return Array.Empty<byte>();
        return _controller.Machine.Memory.GetMemoryPage(start, length);
    }

    public void WriteMemory(ushort start, byte[] data)
    {
        if (_controller.Machine is null) return;
        for (int i = 0; i < data.Length; i++)
            _controller.Machine.Memory.WriteByte((ushort)(start + i), data[i]);
    }

    public bool LoadBinary(byte[] data, ushort origin)
    {
        if (_controller.Machine is null) return false;
        for (int i = 0; i < data.Length; i++)
            _controller.Machine.Memory.WriteByte((ushort)(origin + i), data[i]);
        Logger.Info("Loaded {Bytes} bytes at 0x{Origin:X4}", data.Length, origin);
        return true;
    }

    public bool LoadBinaryToRom(byte[] data, ushort romStart)
    {
        if (_controller.Machine is null) return false;
        _controller.Machine.Memory.LoadRom(romStart, data);

        if (_controller.Machine.Profile.Memory?.Vectors is not null)
        {
            _controller.Machine.Memory.WriteByte(0xFFFC, (byte)(romStart & 0xFF));
            _controller.Machine.Memory.WriteByte(0xFFFD, (byte)((romStart >> 8) & 0xFF));
        }

        Logger.Info("Loaded ROM {Bytes} bytes at 0x{RomStart:X4}", data.Length, romStart);
        return true;
    }

    public bool CompileAndLoad(string source)
    {
        var (binary, errors) = _assembler.Assemble(source, 0x8000);
        if (errors.Count > 0)
        {
            Logger.Error("Assembly errors: {Errors}", string.Join("; ", errors));
            return false;
        }

        if (_controller.Machine is null) return false;
        _controller.Machine.Memory.LoadRom(0x8000, binary);
        _controller.Machine.Memory.WriteByte(0xFFFC, 0x00);
        _controller.Machine.Memory.WriteByte(0xFFFD, 0x80);
        _controller.Machine.Reset();
        Logger.Info("Compiled and loaded {Bytes} bytes at 0x8000", binary.Length);
        return true;
    }

    private static void TryLoadProfileRoms(ComputerMachine machine, ComputerProfile profile, string? profileFilePath = null)
    {
        if (profile.Memory?.Rom is null) return;

        string? profileDir = profileFilePath is not null
            ? Path.GetDirectoryName(Path.GetFullPath(profileFilePath))
            : null;

        foreach (var rom in profile.Memory.Rom)
        {
            if (string.IsNullOrEmpty(rom.File)) continue;

            string romPath = ResolveRomPath(rom.File, profileDir);
            Logger.Info("ROM: expected path={Path}, exists={Exists}", romPath, File.Exists(romPath));

            if (!File.Exists(romPath))
            {
                Logger.Warn("ROM file not found: {Path}", romPath);
                continue;
            }

            try
            {
                byte[] data = File.ReadAllBytes(romPath);

                if (!TryParseRomStart(rom, out var start, out var parseError))
                {
                    Logger.Warn("ROM {File}: invalid start address '{Start}': {Error}", rom.File, rom.Start, parseError);
                    continue;
                }

                ushort declaredSize = 0;
                if (!string.IsNullOrEmpty(rom.Size))
                {
                    try { declaredSize = ComputerProfileLoader.ParseHex(rom.Size); }
                    catch { Logger.Warn("ROM {File}: invalid declared size '{Size}'", rom.File, rom.Size); }
                }

                if (declaredSize > 0 && data.Length < declaredSize)
                    Logger.Warn("ROM {File}: {DataBytes} bytes is shorter than declared size {DeclaredSize}",
                        rom.File, data.Length, declaredSize);

                if (declaredSize > 0 && data.Length > declaredSize)
                    Logger.Warn("ROM {File}: {DataBytes} bytes exceeds declared size {DeclaredSize}, will truncate",
                        rom.File, data.Length, declaredSize);

                machine.Memory.LoadRom(start, data);
                Logger.Info("Loaded ROM: {File} ({Bytes} bytes at 0x{Addr:X4})", rom.File, data.Length, start);
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to load ROM {File}: {Error}", rom.File, ex.Message);
            }
        }
    }

    private static string ResolveRomPath(string romFile, string? profileDir)
    {
        if (Path.IsPathRooted(romFile))
            return romFile;

        if (profileDir is not null)
        {
            string relativeToProfile = Path.GetFullPath(Path.Combine(profileDir, romFile));
            if (File.Exists(relativeToProfile))
                return relativeToProfile;
        }

        string repoRoot = FindRepoRoot(Directory.GetCurrentDirectory());
        string relativeToRepo = Path.GetFullPath(Path.Combine(repoRoot, romFile));
        if (File.Exists(relativeToRepo))
            return relativeToRepo;

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), romFile));
    }

    private static string FindRepoRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            if (dir.GetFiles(".gitignore").Length > 0 ||
                dir.GetFiles("CmosCpuSimulator.slnx").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        return startDir;
    }

    private static bool TryParseRomStart(ComputerProfileMemorySection rom, out ushort start, out string? error)
    {
        start = 0;
        error = null;
        if (string.IsNullOrWhiteSpace(rom.Start))
        {
            error = "ROM section has no 'start' address";
            return false;
        }
        try
        {
            start = ComputerProfileLoader.ParseHex(rom.Start);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
