using System;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Services.Banking;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Interfaces;
using Xunit;

namespace Application.Tests;

public class DepositAppServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ISavingsAccountRepository> _savingsAccounts;
    private readonly Mock<ITransactionRepository> _transactions;
    private readonly Mock<IEmailService> _emailService;
    private readonly DepositAppService _service;
    private readonly Guid _tellerId = Guid.NewGuid();

    public DepositAppServiceTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _savingsAccounts = new Mock<ISavingsAccountRepository>();
        _transactions = new Mock<ITransactionRepository>();
        _emailService = new Mock<IEmailService>();

        _unitOfWork.SetupGet(u => u.SavingsAccounts).Returns(_savingsAccounts.Object);
        _unitOfWork.SetupGet(u => u.Transactions).Returns(_transactions.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _service = new DepositAppService(_unitOfWork.Object, _emailService.Object, NullLogger<DepositAppService>.Instance);
    }

    [Fact]
    public async Task CreateDeposit_ValidActiveAccount_CreditsBalanceAndSendsEmail()
    {
        var accountNumber = "123456789";
        var account = new SavingsAccount
        {
            Id = Guid.NewGuid(),
            AccountNumber = accountNumber,
            Balance = 1000m,
            Status = AccountStatus.Activa,
            Client = new ApplicationUser { FirstName = "Juan", LastName = "Perez", Email = "juan@test.com" }
        };

        _savingsAccounts.Setup(s => s.GetByAccountNumberAsync(accountNumber))
            .ReturnsAsync(account);
        _emailService.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var dto = new CreateDepositDto { AccountNumber = accountNumber, Amount = 500m };
        var result = await _service.CreateDepositAsync(_tellerId, dto);

        Assert.True(result.Success);
        Assert.True(result.EmailSent);
        Assert.Equal(1500m, account.Balance);
        _savingsAccounts.Verify(s => s.Update(account), Times.Once);
        _transactions.Verify(t => t.AddAsync(It.Is<Transaction>(tx => tx.Amount == 500m && tx.Type == TransactionType.CRÉDITO)), Times.Once);
        _emailService.Verify(e => e.SendAsync("juan@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateDeposit_NonPositiveAmount_ReturnsError()
    {
        var dto = new CreateDepositDto { AccountNumber = "123456789", Amount = 0m };
        var result = await _service.CreateDepositAsync(_tellerId, dto);

        Assert.False(result.Success);
        Assert.Contains("mayor que cero", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateDeposit_InactiveOrNonExistentAccount_ReturnsError()
    {
        var accountNumber = "123456789";
        var account = new SavingsAccount
        {
            Id = Guid.NewGuid(),
            AccountNumber = accountNumber,
            Status = AccountStatus.Cancelada
        };

        _savingsAccounts.Setup(s => s.GetByAccountNumberAsync(accountNumber))
            .ReturnsAsync(account);

        var dto = new CreateDepositDto { AccountNumber = accountNumber, Amount = 200m };
        var result = await _service.CreateDepositAsync(_tellerId, dto);

        Assert.False(result.Success);
        Assert.Contains("no corresponde a una cuenta válida", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
