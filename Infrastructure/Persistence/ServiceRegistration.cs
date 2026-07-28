using Application.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistenceLayer(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, Repositories.UnitOfWork>();
        return services;
    }
}
