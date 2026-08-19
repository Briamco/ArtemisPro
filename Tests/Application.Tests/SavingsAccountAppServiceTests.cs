using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Services.Banking;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Moq;
using Xunit;

namespace Application.Tests;

public class SavingsAccountAppServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ISavingsAccountRepository> _savingsAccounts;
    private readonly Mock<IApplicationUserRepository> _users;
    private readonly Mock<ITransactionRepository> _transactions;
    private readonly Mock<ILoanRepository> _loans;
    private readonly Mock<IMapper> _mapper;
    private readonly SavingsAccountAppService _service;

    public SavingsAccountAppServiceTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _savingsAccounts = new Mock<ISavingsAccountRepository>();
        _users = new Mock<IApplicationUserRepository>();
        _transactions = new Mock<ITransactionRepository>();
        _loans = new Mock<ILoanRepository>();
        _mapper = new Mock<IMapper>();

        _unitOfWork.SetupGet(u => u.SavingsAccounts).Returns(_savingsAccounts.Object);
        _unitOfWork.SetupGet(u => u.Users).Returns(_users.Object);
        _unitOfWork.SetupGet(u => u.Transactions).Returns(_transactions.Object);
        _unitOfWork.SetupGet(u => u.Loans).Returns(_loans.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _service = new SavingsAccountAppService(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task CreateSavingsAccount_ActiveClientWithPrimaryAccount_CreatesSecondarySuccessfully()
    {
        var clientId = Guid.NewGuid();
        var client = new ApplicationUser { Id = clientId, IsActive = true, FirstName = "Juan", LastName = "Perez" };
        var primaryAccount = new SavingsAccount { Id = Guid.NewGuid(), ClientId = clientId, AccountType = AccountType.Principal, Status = AccountStatus.Activa };

        _users.Setup(u => u.FindAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(new List<ApplicationUser> { client });
        _savingsAccounts.Setup(s => s.GetPrimaryByClientIdAsync(clientId))
            .ReturnsAsync(primaryAccount);
        _savingsAccounts.Setup(s => s.ExistsAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
            .ReturnsAsync(false);
        _loans.Setup(l => l.ExistsAsync(It.IsAny<Expression<Func<Loan, bool>>>()))
            .ReturnsAsync(false);

        var dto = new CreateSavingsAccountDto { ClientId = clientId, InitialBalance = 1500m };
        var (success, error) = await _service.CreateSavingsAccountAsync(dto);

        Assert.True(success);
        Assert.Null(error);
        _savingsAccounts.Verify(s => s.AddAsync(It.Is<SavingsAccount>(a => a.AccountType == AccountType.Secundaria && a.Balance == 1500m)), Times.Once);
        _transactions.Verify(t => t.AddAsync(It.Is<Transaction>(tx => tx.Amount == 1500m && tx.Type == TransactionType.CRÉDITO)), Times.Once);
    }

    [Fact]
    public async Task CreateSavingsAccount_InactiveClient_ReturnsError()
    {
        var clientId = Guid.NewGuid();
        var client = new ApplicationUser { Id = clientId, IsActive = false };

        _users.Setup(u => u.FindAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(new List<ApplicationUser> { client });

        var dto = new CreateSavingsAccountDto { ClientId = clientId, InitialBalance = 500m };
        var (success, error) = await _service.CreateSavingsAccountAsync(dto);

        Assert.False(success);
        Assert.Contains("no está activo", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateSavingsAccount_ClientWithoutPrimaryAccount_ReturnsError()
    {
        var clientId = Guid.NewGuid();
        var client = new ApplicationUser { Id = clientId, IsActive = true };

        _users.Setup(u => u.FindAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(new List<ApplicationUser> { client });
        _savingsAccounts.Setup(s => s.GetPrimaryByClientIdAsync(clientId))
            .ReturnsAsync((SavingsAccount?)null);

        var dto = new CreateSavingsAccountDto { ClientId = clientId, InitialBalance = 500m };
        var (success, error) = await _service.CreateSavingsAccountAsync(dto);

        Assert.False(success);
        Assert.Contains("principal", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelSavingsAccount_SecondaryAccountWithBalance_TransfersBalanceToPrimaryAccountAndCancels()
    {
        var clientId = Guid.NewGuid();
        var secondaryAccountId = Guid.NewGuid();
        var secondaryAccount = new SavingsAccount
        {
            Id = secondaryAccountId,
            AccountNumber = "111222333",
            ClientId = clientId,
            Balance = 3000m,
            AccountType = AccountType.Secundaria,
            Status = AccountStatus.Activa
        };
        var primaryAccount = new SavingsAccount
        {
            Id = Guid.NewGuid(),
            AccountNumber = "999888777",
            ClientId = clientId,
            Balance = 1000m,
            AccountType = AccountType.Principal,
            Status = AccountStatus.Activa
        };

        _savingsAccounts.Setup(s => s.GetByIdAsync(secondaryAccountId))
            .ReturnsAsync(secondaryAccount);
        _savingsAccounts.Setup(s => s.GetPrimaryByClientIdAsync(clientId))
            .ReturnsAsync(primaryAccount);

        var (success, error) = await _service.CancelSavingsAccountAsync(secondaryAccountId);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(AccountStatus.Cancelada, secondaryAccount.Status);
        Assert.Equal(0m, secondaryAccount.Balance);
        Assert.Equal(4000m, primaryAccount.Balance);
        _transactions.Verify(t => t.AddAsync(It.Is<Transaction>(tx => tx.Type == TransactionType.DÉBITO && tx.Amount == 3000m)), Times.Once);
        _transactions.Verify(t => t.AddAsync(It.Is<Transaction>(tx => tx.Type == TransactionType.CRÉDITO && tx.Amount == 3000m)), Times.Once);
    }

    [Fact]
    public async Task CancelSavingsAccount_PrimaryAccount_ReturnsError()
    {
        var primaryAccountId = Guid.NewGuid();
        var primaryAccount = new SavingsAccount
        {
            Id = primaryAccountId,
            AccountType = AccountType.Principal,
            Status = AccountStatus.Activa
        };

        _savingsAccounts.Setup(s => s.GetByIdAsync(primaryAccountId))
            .ReturnsAsync(primaryAccount);

        var (success, error) = await _service.CancelSavingsAccountAsync(primaryAccountId);

        Assert.False(success);
        Assert.Equal("Las cuentas principales no pueden ser canceladas.", error);
    }

    [Fact]
    public async Task CancelSavingsAccount_AlreadyCancelled_ReturnsError()
    {
        var accountId = Guid.NewGuid();
        var account = new SavingsAccount
        {
            Id = accountId,
            AccountType = AccountType.Secundaria,
            Status = AccountStatus.Cancelada
        };

        _savingsAccounts.Setup(s => s.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        var (success, error) = await _service.CancelSavingsAccountAsync(accountId);

        Assert.False(success);
        Assert.Contains("ya se encuentra cancelada", error, StringComparison.OrdinalIgnoreCase);
    }
}
