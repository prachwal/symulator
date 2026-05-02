using CmosCpu.BlazorApp.Services;
using CmosCpu.Computer;
using CmosCpu.Core;
using CmosCpu.Runtime;

namespace CmosCpu.BlazorApp.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimulatorServices(this IServiceCollection services)
    {
        var simulator = new Simulator();
        services.AddSingleton(simulator);
        services.AddSingleton<ISimulator>(simulator);

        services.AddSingleton<IProgramUploadService, ProgramUploadService>();
        services.AddSingleton<IBlazorSimulationController, BlazorSimulationController>();

        services.AddSingleton<IDebugger, DebuggerService>();
        services.AddSingleton<IMachineProfile, Educational8BitMachineProfile>();
        services.AddSingleton<IMachineBuilder, MachineBuilder>();
        services.AddSingleton<IMachine>(sp =>
        {
            var profile = sp.GetRequiredService<IMachineProfile>();
            var builder = sp.GetRequiredService<IMachineBuilder>();
            var dbg = sp.GetRequiredService<IDebugger>();
            profile.Configure(builder);
            builder.WithDebugger(dbg);
            return builder.Build();
        });
        services.AddSingleton<IMemoryMap>(sp =>
        {
            var profile = sp.GetRequiredService<IMachineProfile>();
            return profile.CreateMemoryMap();
        });

        services.AddSingleton<IEmulatorUiSettings, EmulatorUiSettings>();

        services.AddSingleton<Retro70Service>();
        services.AddSingleton<ComputerRunController>();

        return services;
    }
}