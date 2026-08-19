using System;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Persistence.IntegrationTests;

public class RepositoryIntegrationTests
{
    [Fact]
    public async Task SavingsAccountRepository_AddAndGetPrimaryByClientId_ReturnsCorrectAccount()
    {
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var uow = TestDbContextFactory.CreateUnitOfWork(context);

        var clientId = Guid.NewGuid();
        var account = new SavingsAccount
        {
            Id = Guid.NewGuid(),
            AccountNumber = "100200300",
            ClientId = clientId,
            Balance = 25000m,
            AccountType = AccountType.Principal,
            Status = AccountStatus.Activa,
            CreatedAt = DateTime.UtcNow
        };

        await uow.SavingsAccounts.AddAsync(account);
        await uow.SaveChangesAsync();

        var retrieved = await uow.SavingsAccounts.GetPrimaryByClientIdAsync(clientId);

        Assert.NotNull(retrieved);
        Assert.Equal("100200300", retrieved.AccountNumber);
        Assert.Equal(AccountType.Principal, retrieved.AccountType);
        Assert.Equal(25000m, retrieved.Balance);
    }

    [Fact]
    public async Task MerchantRepository_GetByRNC_ReturnsMatchingMerchant()
    {
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var uow = TestDbContextFactory.CreateUnitOfWork(context);

        var merchant = new Merchant
        {
            Id = Guid.NewGuid(),
            Name = "Farmacia Central",
            Email = "farmacia@central.com",
            PhoneNumber = "8095554321",
            RNC = "131445566",
            Status = MerchantStatus.Activo,
            CreatedAt = DateTime.UtcNow
        };

        await uow.Merchants.AddAsync(merchant);
        await uow.SaveChangesAsync();

        var found = await uow.Merchants.GetByRNCAsync("131445566");

        Assert.NotNull(found);
        Assert.Equal("Farmacia Central", found.Name);
    }

    [Fact]
    public async Task CreditCardRepository_GetByCardNumber_ReturnsCardWithClient()
    {
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var uow = TestDbContextFactory.CreateUnitOfWork(context);

        var clientId = Guid.NewGuid();
        var client = new ApplicationUser
        {
            Id = clientId,
            UserName = "mrodriguez",
            FirstName = "Manuel",
            LastName = "Rodriguez",
            Email = "manuel@test.com",
            Cedula = "00199998888"
        };
        await context.Users.AddAsync(client);

        var card = new CreditCard
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            CardNumber = "4500123456789012",
            Limit = 50000m,
            Debt = 5000m,
            ExpirationDate = "05/29",
            CvcHash = "dummyHash",
            Status = CardStatus.Activa,
            CreatedAt = DateTime.UtcNow
        };

        await uow.CreditCards.AddAsync(card);
        await uow.SaveChangesAsync();

        var foundCard = await uow.CreditCards.GetByCardNumberAsync("4500123456789012");

        Assert.NotNull(foundCard);
        Assert.Equal(50000m, foundCard.Limit);
        Assert.Equal(5000m, foundCard.Debt);
        Assert.NotNull(foundCard.Client);
        Assert.Equal("Manuel", foundCard.Client.FirstName);
    }

    [Fact]
    public async Task BeneficiaryRepository_GetByClientAndAccount_ReturnsCorrectBeneficiary()
    {
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var uow = TestDbContextFactory.CreateUnitOfWork(context);

        var clientId = Guid.NewGuid();
        var beneficiary = new Beneficiary
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            BeneficiaryAccountNumber = "987654321",
            Alias = "Hermano",
            Status = BeneficiaryStatus.Activo,
            CreatedAt = DateTime.UtcNow
        };

        await uow.Beneficiaries.AddAsync(beneficiary);
        await uow.SaveChangesAsync();

        var found = await uow.Beneficiaries.GetByClientAndAccountAsync(clientId, "987654321");

        Assert.NotNull(found);
        Assert.Equal("Hermano", found.Alias);
    }
}
