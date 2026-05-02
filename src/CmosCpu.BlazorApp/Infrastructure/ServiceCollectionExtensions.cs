using CmosCpu.BlazorApp.Services;
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

        services.AddSingleton<IMachineProfile, Educational8BitMachineProfile>();
        services.AddSingleton<IMachineBuilder, MachineBuilder>();
        services.AddSingleton<IMachine>(sp =>
        {
            var profile = sp.GetRequiredService<IMachineProfile>();
            var builder = sp.GetRequiredService<IMachineBuilder>();
            profile.Configure(builder);
            return builder.Build();
        });

        return services;
    }
}
