using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Themes.Fluent;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Config;
using Symulator.Application.Abstractions;
using Symulator.Application.Services;
using Symulator.Avalonia.Services;
using Symulator.Avalonia.ViewModels;
using Symulator.Avalonia.Views;
using Symulator.Machines.Apple1.Module;
using Symulator.Machines.Apple1.Views;
using Symulator.Machines.Kim1.Module;
using Symulator.Machines.Kim1.Views;

namespace Symulator.Avalonia;

public class App : global::Avalonia.Application
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private ServiceProvider? _services;

    public override void Initialize()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "NLog.config");
        var logsPath = Path.Combine(AppContext.BaseDirectory, "logs");
        if (File.Exists(configPath))
        {
            LogManager.Configuration = new XmlLoggingConfiguration(configPath);
        }
        Directory.CreateDirectory(logsPath);
        Logger.Info("Starting Symulator.Avalonia");
        Logger.Debug("NLog config path: {ConfigPath}", configPath);
        Logger.Debug("Logs directory: {LogsPath}", logsPath);
        Logger.Debug("App base directory: {BaseDirectory}", AppContext.BaseDirectory);

        Styles.Add(new FluentTheme());
        Logger.Debug("FluentTheme registered");

        var collection = new ServiceCollection();
        collection.AddSingleton<UiErrorService>();
        collection.AddSingleton<IUiErrorService>(sp => sp.GetRequiredService<UiErrorService>());
        collection.AddSingleton<IMachineNotificationSink>(sp => sp.GetRequiredService<UiErrorService>());
        collection.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        collection.AddSingleton<IMachineModule, Apple1MachineModule>();
        collection.AddSingleton<IMachineModule, Kim1MachineModule>();
        collection.AddSingleton<MachineCatalog>();
        collection.AddSingleton<IMachineCatalog>(sp => sp.GetRequiredService<MachineCatalog>());
        collection.AddSingleton<IEmulatorController, EmulatorController>();
        collection.AddTransient<MainWindowViewModel>();
        _services = collection.BuildServiceProvider();
        Logger.Debug("Dependency injection container built");

        DataTemplates.Add(new FuncDataTemplate<Apple1WorkspaceViewModel>((vm, _) => new Apple1WorkspaceView { DataContext = vm }));
        DataTemplates.Add(new FuncDataTemplate<Kim1WorkspaceViewModel>((vm, _) => new Kim1WorkspaceView { DataContext = vm }));
        Logger.Debug("Workspace data templates registered for Apple-1 and KIM-1");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = _services!.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = vm };
            Logger.Debug("MainWindow created and DataContext assigned");
        }

        base.OnFrameworkInitializationCompleted();
    }
}
