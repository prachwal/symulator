using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Symulator.Application.Abstractions;
using Symulator.Application.Services;
using Symulator.Avalonia.ViewModels;
using Symulator.Avalonia.Views;
using Symulator.Machines.Apple1.Module;
using Symulator.Machines.Apple1.Views;
using Symulator.Machines.Kim1.Module;
using Symulator.Machines.Kim1.Views;

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
        collection.AddSingleton<IUiErrorService, UiErrorService>();
        collection.AddTransient<MainWindowViewModel>();
        _services = collection.BuildServiceProvider();

        DataTemplates.Add(new FuncDataTemplate<Apple1WorkspaceViewModel>((vm, _) => new Apple1WorkspaceView { DataContext = vm }));
        DataTemplates.Add(new FuncDataTemplate<Kim1WorkspaceViewModel>((vm, _) => new Kim1WorkspaceView { DataContext = vm }));
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
