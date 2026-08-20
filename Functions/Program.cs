using Application;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Persistence;
using Persistence.Contexts;
using Shared;

try
{
    DotNetEnv.Env.TraversePath().Load();
}
catch { }

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")))
{
    var candidatePaths = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "Functions", ".env"),
        Path.Combine(AppContext.BaseDirectory, ".env")
    };
    foreach (var path in candidatePaths)
    {
        if (File.Exists(path))
        {
            try { DotNetEnv.Env.Load(path); } catch { }
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")))
                break;
        }
    }
}

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((hostContext, config) =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((hostContext, services) =>
    {
        var config = hostContext.Configuration;
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? config.GetConnectionString("DefaultConnection")
            ?? config["Values:DefaultConnection"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));
        }
        else
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder => builder.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));
        }

        // Shared infrastructure configuration
        services.AddApplicationLayer();
        services.AddPersistenceLayer();
        services.AddSharedInfrastructure(config);
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
    })
    .Build();

await host.RunAsync();
