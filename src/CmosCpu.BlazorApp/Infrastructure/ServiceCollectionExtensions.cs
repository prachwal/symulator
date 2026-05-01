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

        return services;
    }
}
