using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Persistence.Contexts;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        LoadEnvironment();

        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? "Server=localhost,1433;Database=ArtemisProDb;User Id=sa;Password=SqlPass12345;TrustServerCertificate=True;Encrypt=False;";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    private static void LoadEnvironment()
    {
        try
        {
            DotNetEnv.Env.TraversePath().Load();
        }
        catch
        {
            // Ignore if traversal fails
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")))
            return;

        var searchDirs = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var baseDir in searchDirs)
        {
            if (string.IsNullOrWhiteSpace(baseDir))
                continue;

            var dir = new DirectoryInfo(baseDir);
            while (dir != null)
            {
                var candidatePaths = new[]
                {
                    Path.Combine(dir.FullName, ".env"),
                    Path.Combine(dir.FullName, "Api", ".env"),
                    Path.Combine(dir.FullName, "Web", ".env")
                };

                foreach (var path in candidatePaths)
                {
                    if (File.Exists(path))
                    {
                        try
                        {
                            DotNetEnv.Env.Load(path);
                            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")))
                                return;
                        }
                        catch
                        {
                            // Try next candidate
                        }
                    }
                }

                dir = dir.Parent;
            }
        }
    }
}
