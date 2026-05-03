using NLog;

namespace Symulator.Application.Assembly;

public interface IAssemblyProgramCatalog
{
    Task<IReadOnlyList<AssemblySourceProgram>> LoadProgramsAsync(string directoryPath, CancellationToken ct = default);
    Task<AssemblySourceProgram> LoadProgramAsync(string id, string directoryPath, CancellationToken ct = default);
}

public sealed class AssemblyProgramCatalog : IAssemblyProgramCatalog
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<IReadOnlyList<AssemblySourceProgram>> LoadProgramsAsync(string directoryPath, CancellationToken ct = default)
    {
        var result = new List<AssemblySourceProgram>();

        if (!Directory.Exists(directoryPath))
        {
            Logger.Warn("ASM program directory not found: {Path}", directoryPath);
            return result;
        }

        foreach (var file in Directory.GetFiles(directoryPath, "*.asm").OrderBy(f => f))
        {
            try
            {
                string id = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                string text = await File.ReadAllTextAsync(file, ct);
                result.Add(new AssemblySourceProgram
                {
                    Id = id,
                    DisplayName = Path.GetFileNameWithoutExtension(file),
                    FilePath = file,
                    SourceText = text
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load ASM program: {Path}", file);
            }
        }

        return result;
    }

    public async Task<AssemblySourceProgram> LoadProgramAsync(string id, string directoryPath, CancellationToken ct = default)
    {
        string file = Path.Combine(directoryPath, id + ".asm");
        string text = await File.ReadAllTextAsync(file, ct);
        return new AssemblySourceProgram
        {
            Id = id,
            DisplayName = id,
            FilePath = file,
            SourceText = text
        };
    }
}
