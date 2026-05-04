using System.Text.Json;
using NLog;

namespace Symulator.Application.Solutions;

public sealed class SolutionDefinitionLoader
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public async Task<SolutionDefinition> LoadForSourceAsync(string sourceFilePath, CancellationToken ct = default)
    {
        var directory = Path.GetDirectoryName(sourceFilePath) ?? string.Empty;
        var id = Path.GetFileNameWithoutExtension(sourceFilePath).ToLowerInvariant();
        var manifestPath = Path.Combine(directory, id + ".solution.json");

        if (!File.Exists(manifestPath))
        {
            Logger.Warn("Solution manifest not found for {Source}. Using fallback profile.", sourceFilePath);
            return SolutionDefinition.CreateFallback(id, Path.GetFileName(sourceFilePath));
        }

        await using var stream = File.OpenRead(manifestPath);
        var definition = await JsonSerializer.DeserializeAsync<SolutionDefinition>(stream, JsonOptions, ct)
            ?? throw new InvalidOperationException($"Solution manifest is empty: {manifestPath}");

        definition.SourceFilePath = manifestPath;

        if (string.IsNullOrWhiteSpace(definition.Id))
            definition.Id = id;

        if (string.IsNullOrWhiteSpace(definition.Name))
            definition.Name = definition.Id;

        if (string.IsNullOrWhiteSpace(definition.Source))
            definition.Source = Path.GetFileName(sourceFilePath);

        return definition;
    }
}
