namespace CmosCpu.BlazorApp.Services;

public interface IProgramUploadService
{
    bool ValidateFile(string fileName, long fileSize, out string? error);
}
