using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Symulator.Application.Abstractions;

namespace Symulator.Machines.Kim1.Module;

public sealed class Kim1WorkspaceViewModel : INotifyPropertyChanged
{
    private readonly IMachineSession _session;
    private string _displayText = "--------";
    private string _lastKey = string.Empty;
    private string _statusText = "Ready";
    private string _portA = "00";
    private string _portB = "00";
    private string _ddra = "00";
    private string _ddrb = "00";

    public Kim1WorkspaceViewModel(IMachineSession session)
    {
        _session = session;
        ResetCommand = new AsyncSessionCommand(() => _session.ExecuteMachineCommandAsync("kim1.reset"));
        KeyPressCommand = new AsyncSessionCommand<string>(key => _session.ExecuteMachineCommandAsync("kim1.press-key", key));
        KeyReleaseCommand = new AsyncSessionCommand<string>(key => _session.ExecuteMachineCommandAsync("kim1.release-key", key));
    }

    public string DisplayText
    {
        get => _displayText;
        set { _displayText = value; OnPropertyChanged(); }
    }

    public string LastKey
    {
        get => _lastKey;
        set { _lastKey = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string PortA { get => _portA; set { _portA = value; OnPropertyChanged(); } }
    public string PortB { get => _portB; set { _portB = value; OnPropertyChanged(); } }
    public string DDRA { get => _ddra; set { _ddra = value; OnPropertyChanged(); } }
    public string DDRB { get => _ddrb; set { _ddrb = value; OnPropertyChanged(); } }

    public ICommand ResetCommand { get; }
    public ICommand KeyPressCommand { get; }
    public ICommand KeyReleaseCommand { get; }

    public void OnKeyPressed(string key)
    {
        LastKey = key;
        var kw = new AsyncSessionCommand<string>(k => _session.ExecuteMachineCommandAsync("kim1.press-key", k));
        kw.Execute(key);
        var rw = new AsyncSessionCommand<string>(k => _session.ExecuteMachineCommandAsync("kim1.release-key", k));
        rw.Execute(key);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

internal sealed class AsyncSessionCommand : ICommand
{
    private readonly Func<Task> _execute;
    private bool _isExecuting;

    public AsyncSessionCommand(Func<Task> execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting;
    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;
        _isExecuting = true;
        RaiseCanExecuteChanged();
        try { await _execute(); } catch { }
        finally { _isExecuting = false; RaiseCanExecuteChanged(); }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

internal sealed class AsyncSessionCommand<T> : ICommand
{
    private readonly Func<T?, Task> _execute;
    private bool _isExecuting;

    public AsyncSessionCommand(Func<T?, Task> execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting;
    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;
        _isExecuting = true;
        RaiseCanExecuteChanged();
        try { await _execute((T?)parameter); } catch { }
        finally { _isExecuting = false; RaiseCanExecuteChanged(); }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
