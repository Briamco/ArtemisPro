using System;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;
using Persistence.Repositories;

namespace Persistence.IntegrationTests;

public static class TestDbContextFactory
{
    public static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static UnitOfWork CreateUnitOfWork(AppDbContext context)
    {
        return new UnitOfWork(context);
    }
}
