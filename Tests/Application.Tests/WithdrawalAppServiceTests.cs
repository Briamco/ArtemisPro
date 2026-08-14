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

public class WithdrawalAppServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ISavingsAccountRepository> _savingsAccounts;
    private readonly Mock<ITransactionRepository> _transactions;
    private readonly Mock<IEmailService> _emailService;
    private readonly WithdrawalAppService _service;
    private readonly Guid _tellerId = Guid.NewGuid();

    public WithdrawalAppServiceTests()
    {
        _savingsAccounts = new Mock<ISavingsAccountRepository>();
        _transactions = new Mock<ITransactionRepository>();
        _emailService = new Mock<IEmailService>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _unitOfWork.SetupGet(u => u.SavingsAccounts).Returns(_savingsAccounts.Object);
        _unitOfWork.SetupGet(u => u.Transactions).Returns(_transactions.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _transactions
            .Setup(r => r.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => t);

        _service = new WithdrawalAppService(
            _unitOfWork.Object,
            _emailService.Object,
            NullLogger<WithdrawalAppService>.Instance);
    }

    private static SavingsAccount BuildActiveAccount(decimal balance, string accountNumber = "123456789")
    {
        return new SavingsAccount
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            AccountNumber = accountNumber,
            Balance = balance,
            AccountType = AccountType.Principal,
            Status = AccountStatus.Activa,
            CreatedAt = DateTime.UtcNow,
            Client = new ApplicationUser
            {
                FirstName = "Juan",
                LastName = "Perez",
                Email = "juan.perez@example.com"
            }
        };
    }

    private static CreateWithdrawalDto BuildDto(string accountNumber, decimal amount)
    {
        return new CreateWithdrawalDto
        {
            AccountNumber = accountNumber,
            Amount = amount
        };
    }

    [Fact]
    public async Task CreateWithdrawalAsync_ValidRequest_WithEmailSent_DebitsBalanceAndApprovesTransaction()
    {
        var account = BuildActiveAccount(balance: 1000m);
        _savingsAccounts.Setup(r => r.GetByAccountNumberAsync(account.AccountNumber)).ReturnsAsync(account);

        var result = await _service.CreateWithdrawalAsync(_tellerId, BuildDto(account.AccountNumber, 100m));

        Assert.True(result.Success);
        Assert.True(result.EmailSent);
        Assert.Equal(900m, account.Balance);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Type == TransactionType.DÉBITO &&
            t.Beneficiary == "RETIRO" &&
            t.Origin == account.AccountNumber &&
            t.Status == TransactionStatus.APROBADA &&
            t.Amount == 100m &&
            t.PerformedById == _tellerId)), Times.Once);
        _emailService.Verify(e => e.SendAsync(
            "juan.perez@example.com",
            "Retiro realizado desde su cuenta 6789",
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateWithdrawalAsync_ValidRequest_WithEmailFailure_DebitsBalanceButReportsEmailNotSent()
    {
        var account = BuildActiveAccount(balance: 1000m);
        _savingsAccounts.Setup(r => r.GetByAccountNumberAsync(account.AccountNumber)).ReturnsAsync(account);
        _emailService
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var result = await _service.CreateWithdrawalAsync(_tellerId, BuildDto(account.AccountNumber, 100m));

        Assert.True(result.Success);
        Assert.False(result.EmailSent);
        Assert.Equal(900m, account.Balance);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t => t.Status == TransactionStatus.APROBADA)), Times.Once);
    }

    [Fact]
    public async Task CreateWithdrawalAsync_InsufficientFunds_RecordsRejectedAttemptWithoutAffectingBalance()
    {
        var account = BuildActiveAccount(balance: 100m);
        _savingsAccounts.Setup(r => r.GetByAccountNumberAsync(account.AccountNumber)).ReturnsAsync(account);

        var result = await _service.CreateWithdrawalAsync(_tellerId, BuildDto(account.AccountNumber, 200m));

        Assert.False(result.Success);
        Assert.Equal("El monto ingresado excede el saldo disponible de la cuenta.", result.Error);
        Assert.Equal(100m, account.Balance);
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Status == TransactionStatus.RECHAZADA &&
            t.Amount == 200m &&
            t.Beneficiary == "RETIRO" &&
            t.PerformedById == _tellerId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateWithdrawalAsync_NonExistentAccount_ReturnsErrorWithoutRecordingTransaction()
    {
        _savingsAccounts.Setup(r => r.GetByAccountNumberAsync(It.IsAny<string>())).ReturnsAsync((SavingsAccount?)null);

        var result = await _service.CreateWithdrawalAsync(_tellerId, BuildDto("000000000", 100m));

        Assert.False(result.Success);
        Assert.Equal("El número de cuenta ingresado no corresponde a una cuenta válida.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateWithdrawalAsync_InactiveAccount_ReturnsErrorWithoutRecordingTransaction()
    {
        var account = BuildActiveAccount(balance: 1000m);
        account.Status = AccountStatus.Cancelada;
        _savingsAccounts.Setup(r => r.GetByAccountNumberAsync(account.AccountNumber)).ReturnsAsync(account);

        var result = await _service.CreateWithdrawalAsync(_tellerId, BuildDto(account.AccountNumber, 100m));

        Assert.False(result.Success);
        Assert.Equal("El número de cuenta ingresado no corresponde a una cuenta válida.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateWithdrawalAsync_NonPositiveAmount_ReturnsErrorWithoutPersistingInvalidTransaction()
    {
        var account = BuildActiveAccount(balance: 1000m);
        _savingsAccounts.Setup(r => r.GetByAccountNumberAsync(account.AccountNumber)).ReturnsAsync(account);

        var result = await _service.CreateWithdrawalAsync(_tellerId, BuildDto(account.AccountNumber, 0m));

        Assert.False(result.Success);
        Assert.Equal("El monto a retirar debe ser mayor que cero.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }
}
