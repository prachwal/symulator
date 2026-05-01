using System.Windows;
using CmosCpu.WpfApp.Services;
using CmosCpu.WpfApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CmosCpu.WpfApp;

public partial class App : Application
{
    private ServiceProvider _serviceProvider = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        var simulator = new Runtime.Simulator();
        services.AddSingleton(simulator);
        services.AddSingleton<Core.ISimulator>(simulator);

        services.AddSingleton<IDispatcherService>(sp =>
            new DispatcherService(Dispatcher));
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IUiSimulationController, UiSimulationController>();

        services.AddTransient<RegistersViewModel>();
        services.AddTransient<FlagsViewModel>();
        services.AddTransient<MemoryViewModel>();
        services.AddTransient<LedViewModel>();
        services.AddTransient<TraceLogViewModel>();
        services.AddTransient<BusViewModel>();
        services.AddTransient<DevicesViewModel>();
        services.AddTransient<InterruptsViewModel>();
        services.AddTransient<MainWindowViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = new MainWindow
        {
            DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
        };
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
