namespace CmosCpu.WpfApp.Services;

public interface IFileDialogService
{
    string? OpenFileDialog(string title, string filter, string? initialDirectory = null);
}
