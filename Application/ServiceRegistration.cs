using Application.Interfaces.Services;
using Application.Mappings;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<IUserAppService, UserAppService>();
        services.AddScoped<IAdminDashboardAppService, AdminDashboardAppService>();
        services.AddScoped<ICreditCardAppService, CreditCardAppService>();
        services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);
        return services;
    }
}
