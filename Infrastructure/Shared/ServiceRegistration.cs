using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Interfaces;
using Shared.Models;
using Shared.Services;

namespace Shared;

public static class ServiceRegistration
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("EmailSetting"));
        services.AddTransient<IEmailService, SmtpEmailService>();
        return services;
    }
}
