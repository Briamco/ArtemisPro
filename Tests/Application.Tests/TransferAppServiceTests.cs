using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Services.Banking;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Interfaces;
using Xunit;

namespace Application.Tests;

public class TransferAppServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ISavingsAccountRepository> _savingsAccountRepoMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<IApplicationUserRepository> _userRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<TransferAppService>> _loggerMock;
    private readonly TransferAppService _transferService;

    public TransferAppServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _savingsAccountRepoMock = new Mock<ISavingsAccountRepository>();
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _userRepoMock = new Mock<IApplicationUserRepository>();
        _mapperMock = new Mock<IMapper>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<TransferAppService>>();

        _unitOfWorkMock.SetupGet(u => u.SavingsAccounts).Returns(_savingsAccountRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Transactions).Returns(_transactionRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Users).Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _transferService = new TransferAppService(
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _emailServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateTransferAsync_OriginNotFound_ReturnsError()
    {
        var clientId = Guid.NewGuid();
        var originId = Guid.NewGuid();
        _savingsAccountRepoMock.Setup(s => s.GetByIdAsync(originId)).ReturnsAsync((SavingsAccount?)null);

        var dto = new CreateTransferDto { OriginAccountId = originId, DestinationAccountId = Guid.NewGuid(), Amount = 500m };
        var result = await _transferService.CreateTransferAsync(clientId, dto);

        Assert.False(result.Success);
        Assert.Equal("La cuenta de origen seleccionada no existe.", result.Error);
    }

    [Fact]
    public async Task CreateTransferAsync_OriginNotBelongingToClient_ReturnsError()
    {
        var clientId = Guid.NewGuid();
        var originId = Guid.NewGuid();
        var origin = new SavingsAccount { Id = originId, ClientId = Guid.NewGuid(), Status = AccountStatus.Activa };
        _savingsAccountRepoMock.Setup(s => s.GetByIdAsync(originId)).ReturnsAsync(origin);

        var dto = new CreateTransferDto { OriginAccountId = originId, DestinationAccountId = Guid.NewGuid(), Amount = 500m };
        var result = await _transferService.CreateTransferAsync(clientId, dto);

        Assert.False(result.Success);
        Assert.Equal("La cuenta de origen debe pertenecer al cliente autenticado.", result.Error);
    }

    [Fact]
    public async Task CreateTransferAsync_SameOriginAndDestination_RejectsAndRecordsTransaction()
    {
        var clientId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = new SavingsAccount { Id = accountId, ClientId = clientId, AccountNumber = "123456789", Status = AccountStatus.Activa };

        _savingsAccountRepoMock.Setup(s => s.GetByIdAsync(accountId)).ReturnsAsync(account);

        var dto = new CreateTransferDto { OriginAccountId = accountId, DestinationAccountId = accountId, Amount = 500m };
        var result = await _transferService.CreateTransferAsync(clientId, dto);

        Assert.False(result.Success);
        Assert.Equal("La cuenta de origen y la cuenta de destino no pueden ser la misma.", result.Error);
        _transactionRepoMock.Verify(t => t.AddAsync(It.Is<Transaction>(tx => tx.Status == TransactionStatus.RECHAZADA)), Times.Once);
    }

    [Fact]
    public async Task CreateTransferAsync_ClientHasLessThanTwoActiveAccounts_RejectsAndRecordsTransaction()
    {
        var clientId = Guid.NewGuid();
        var originId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var origin = new SavingsAccount { Id = originId, ClientId = clientId, AccountNumber = "111111111", Status = AccountStatus.Activa };
        var dest = new SavingsAccount { Id = destId, ClientId = clientId, AccountNumber = "222222222", Status = AccountStatus.Activa };

        _savingsAccountRepoMock.Setup(s => s.GetByIdAsync(originId)).ReturnsAsync(origin);
        _savingsAccountRepoMock.Setup(s => s.GetByIdAsync(destId)).ReturnsAsync(dest);
        _savingsAccountRepoMock.Setup(s => s.GetByClientIdAsync(clientId)).ReturnsAsync(new List<SavingsAccount> { origin });

        var dto = new CreateTransferDto { OriginAccountId = originId, DestinationAccountId = destId, Amount = 500m };
        var result = await _transferService.CreateTransferAsync(clientId, dto);

        Assert.False(result.Success);
        Assert.Equal("Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas.", result.Error);
        _transactionRepoMock.Verify(t => t.AddAsync(It.Is<Transaction>(tx => tx.Status == TransactionStatus.RECHAZADA)), Times.Once);
    }

    [Fact]
    public async Task CreateTransferAsync_InsufficientFunds_RejectsAndRecordsTransaction()
    {
        var clientId = Guid.NewGuid();
        var originId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var origin = new SavingsAccount { Id = originId, ClientId = clientId, AccountNumber = "111111111", Status = AccountStatus.Activa, Balance = 100m };
        var dest = new SavingsAccount { Id = destId, ClientId = clientId, AccountNumber = "222222222", Status = AccountStatus.Activa, Balance = 50m };

        _savingsAccountRepoMock.Setup(s => s.GetByIdAsync(originId)).ReturnsAsync(origin);
        _savingsAccountRepoMock.Setup(s => s.GetByIdAsync(destId)).ReturnsAsync(dest);
        _savingsAccountRepoMock.Setup(s => s.GetByClientIdAsync(clientId)).ReturnsAsync(new List<SavingsAccount> { origin, dest });

        var dto = new CreateTransferDto { OriginAccountId = originId, DestinationAccountId = destId, Amount = 500m };
        var result = await _transferService.CreateTransferAsync(clientId, dto);

        Assert.False(result.Success);
        Assert.Equal("No dispone del monto requerido en la cuenta seleccionada.", result.Error);
        _transactionRepoMock.Verify(t => t.AddAsync(It.Is<Transaction>(tx => tx.Status == TransactionStatus.RECHAZADA)), Times.Once);
    }

    [Fact]
    public async Task CreateTransferAsync_ValidTransfer_TransfersBalanceAndRecordsDebitAndCreditTransactions()
    {
        var clientId = Guid.NewGuid();
        var originId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var origin = new SavingsAccount { Id = originId, ClientId = clientId, AccountNumber = "111111111", Status = AccountStatus.Activa, Balance = 1000m };
        var dest = new SavingsAccount { Id = destId, ClientId = clientId, AccountNumber = "222222222", Status = AccountStatus.Activa, Balance = 500m };
        var client = new ApplicationUser { Id = clientId, FirstName = "Ana", LastName = "Gomez", Email = "ana@test.com" };

        _savingsAccountRepoMock.Setup(s => s.GetByIdAsync(originId)).ReturnsAsync(origin);
        _savingsAccountRepoMock.Setup(s => s.GetByIdAsync(destId)).ReturnsAsync(dest);
        _savingsAccountRepoMock.Setup(s => s.GetByClientIdAsync(clientId)).ReturnsAsync(new List<SavingsAccount> { origin, dest });
        _userRepoMock.Setup(u => u.GetByIdAsync(clientId)).ReturnsAsync(client);

        var dto = new CreateTransferDto { OriginAccountId = originId, DestinationAccountId = destId, Amount = 300m };
        var result = await _transferService.CreateTransferAsync(clientId, dto);

        Assert.True(result.Success);
        Assert.Equal(700m, origin.Balance);
        Assert.Equal(800m, dest.Balance);
        _savingsAccountRepoMock.Verify(s => s.Update(origin), Times.Once);
        _savingsAccountRepoMock.Verify(s => s.Update(dest), Times.Once);
        _transactionRepoMock.Verify(t => t.AddAsync(It.Is<Transaction>(tx => tx.Type == TransactionType.DÉBITO && tx.Amount == 300m)), Times.Once);
        _transactionRepoMock.Verify(t => t.AddAsync(It.Is<Transaction>(tx => tx.Type == TransactionType.CRÉDITO && tx.Amount == 300m)), Times.Once);
    }
}
