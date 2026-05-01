namespace CmosCpu.BlazorApp.Services;

public class ProgramUploadService : IProgramUploadService
{
    private const long MaxFileSize = 1 * 1024 * 1024; // 1 MB

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".asm",
        ".bin"
    };

    public bool ValidateFile(string fileName, long fileSize, out string? error)
    {
        if (fileSize > MaxFileSize)
        {
            error = $"File too large ({fileSize} bytes). Maximum is {MaxFileSize} bytes.";
            return false;
        }

        string ext = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(ext))
        {
            error = $"Invalid file type '{ext}'. Allowed: .asm, .bin";
            return false;
        }

        error = null;
        return true;
    }
}
