using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Symulator.Application.Abstractions;
using Symulator.Application.Services;
using Symulator.Avalonia.Controls.Apple1;
using Symulator.Avalonia.Controls.Kim1;
using Symulator.Avalonia.ViewModels;
using Symulator.Avalonia.Views;
using Symulator.Machines.Apple1.Module;
using Symulator.Machines.Kim1.Module;

namespace Symulator.Avalonia;

public class App : global::Avalonia.Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());

        var collection = new ServiceCollection();
        collection.AddSingleton<IMachineModule, Apple1MachineModule>();
        collection.AddSingleton<IMachineModule, Kim1MachineModule>();
        collection.AddSingleton<MachineCatalog>();
        collection.AddSingleton<IMachineCatalog>(sp => sp.GetRequiredService<MachineCatalog>());
        collection.AddSingleton<IEmulatorController, EmulatorController>();
        collection.AddTransient<MainWindowViewModel>();
        _services = collection.BuildServiceProvider();

        DataTemplates.Add(new FuncDataTemplate<Apple1TerminalPanelViewModel>((vm, _) => new Apple1TerminalPanelView { DataContext = vm }));
        DataTemplates.Add(new FuncDataTemplate<Apple1BootPanelViewModel>((vm, _) => new Apple1BootPanelView { DataContext = vm }));
        DataTemplates.Add(new FuncDataTemplate<Kim1KeypadViewModel>((vm, _) => new Kim1KeypadView { DataContext = vm }));
        DataTemplates.Add(new FuncDataTemplate<Kim1LedDisplayViewModel>((vm, _) => new Kim1LedDisplayView { DataContext = vm }));
        DataTemplates.Add(new FuncDataTemplate<Kim1RiotStatusViewModel>((vm, _) => new Kim1RiotStatusView { DataContext = vm }));
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = _services!.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = vm };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
