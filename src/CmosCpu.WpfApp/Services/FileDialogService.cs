using Microsoft.Win32;

namespace CmosCpu.WpfApp.Services;

public class FileDialogService : IFileDialogService
{
    public string? OpenFileDialog(string title, string filter, string? initialDirectory = null)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            InitialDirectory = initialDirectory ?? Environment.CurrentDirectory,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
