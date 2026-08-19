using System;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Persistence.IntegrationTests;

public class UnitOfWorkIntegrationTests
{
    [Fact]
    public async Task UnitOfWork_SaveMultipleEntities_PersistsAllCorrectly()
    {
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var uow = TestDbContextFactory.CreateUnitOfWork(context);

        var clientId = Guid.NewGuid();
        var client = new ApplicationUser
        {
            Id = clientId,
            UserName = "pedrog",
            FirstName = "Pedro",
            LastName = "Gomez",
            Email = "pedro@test.com",
            Cedula = "40212345678"
        };
        await context.Users.AddAsync(client);

        var account = new SavingsAccount
        {
            Id = Guid.NewGuid(),
            AccountNumber = "555666777",
            ClientId = clientId,
            Balance = 10000m,
            AccountType = AccountType.Principal,
            Status = AccountStatus.Activa,
            CreatedAt = DateTime.UtcNow
        };
        await uow.SavingsAccounts.AddAsync(account);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            SavingsAccountId = account.Id,
            Amount = 10000m,
            Type = TransactionType.CRÉDITO,
            Origin = "DEPÓSITO",
            Beneficiary = account.AccountNumber,
            Status = TransactionStatus.APROBADA,
            Date = DateTime.UtcNow
        };
        await uow.Transactions.AddAsync(transaction);

        var rows = await uow.SaveChangesAsync();

        Assert.True(rows > 0);
        var retrievedAccount = await uow.SavingsAccounts.GetByIdAsync(account.Id);
        Assert.NotNull(retrievedAccount);
        Assert.Equal(10000m, retrievedAccount.Balance);

        var retrievedTransactions = await uow.Transactions.GetBySavingsAccountIdAsync(account.Id);
        Assert.Single(retrievedTransactions);
    }

    [Fact]
    public async Task UnitOfWork_GetActiveClientsCount_ReturnsAccurateCount()
    {
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var uow = TestDbContextFactory.CreateUnitOfWork(context);

        var roleId = Guid.NewGuid();
        var clientRole = new Microsoft.AspNetCore.Identity.IdentityRole<Guid>
        {
            Id = roleId,
            Name = "Cliente",
            NormalizedName = "CLIENTE"
        };
        await context.Roles.AddAsync(clientRole);

        var user1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "user1",
            FirstName = "User",
            LastName = "One",
            Email = "u1@test.com",
            Cedula = "00100000001",
            IsActive = true
        };
        var user2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "user2",
            FirstName = "User",
            LastName = "Two",
            Email = "u2@test.com",
            Cedula = "00100000002",
            IsActive = false
        };

        await context.Users.AddRangeAsync(user1, user2);
        await context.UserRoles.AddRangeAsync(
            new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid> { RoleId = roleId, UserId = user1.Id },
            new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid> { RoleId = roleId, UserId = user2.Id }
        );
        await uow.SaveChangesAsync();

        var count = await uow.GetActiveClientsCountAsync();

        Assert.Equal(1, count);
    }
}
