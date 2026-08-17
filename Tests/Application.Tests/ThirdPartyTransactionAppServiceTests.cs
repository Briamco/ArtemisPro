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

public class ThirdPartyTransactionAppServiceTests
{
    private const string SourceAccountNumber = "111222333";
    private const string DestinationAccountNumber = "444555666";

    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ISavingsAccountRepository> _savingsAccounts;
    private readonly Mock<ITransactionRepository> _transactions;
    private readonly Mock<IEmailService> _emailService;
    private readonly ThirdPartyTransactionAppService _service;
    private readonly Guid _tellerId = Guid.NewGuid();

    public ThirdPartyTransactionAppServiceTests()
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

        _service = new ThirdPartyTransactionAppService(
            _unitOfWork.Object,
            _emailService.Object,
            NullLogger<ThirdPartyTransactionAppService>.Instance);
    }

    // --- Helpers ---

    private static SavingsAccount BuildActiveAccount(string accountNumber, decimal balance = 5000m, Guid? clientId = null, string firstName = "Juan", string lastName = "Perez")
    {
        var cid = clientId ?? Guid.NewGuid();
        return new SavingsAccount
        {
            Id = Guid.NewGuid(),
            ClientId = cid,
            AccountNumber = accountNumber,
            Balance = balance,
            AccountType = AccountType.Principal,
            Status = AccountStatus.Activa,
            CreatedAt = DateTime.UtcNow,
            Client = new ApplicationUser
            {
                Id = cid,
                FirstName = firstName,
                LastName = lastName,
                Email = $"{firstName}.{lastName}@example.com".ToLowerInvariant()
            }
        };
    }

    private static CreateThirdPartyTransactionDto BuildDto(decimal amount = 1000m, string source = SourceAccountNumber, string destination = DestinationAccountNumber)
    {
        return new CreateThirdPartyTransactionDto
        {
            SourceAccountNumber = source,
            DestinationAccountNumber = destination,
            Amount = amount
        };
    }

    private void SetupRepositories(SavingsAccount? source, SavingsAccount? destination)
    {
        _savingsAccounts
            .Setup(r => r.GetByAccountNumberAsync(It.IsAny<string>()))
            .ReturnsAsync((string accountNumber) =>
            {
                if (source != null && accountNumber == source.AccountNumber) return source;
                if (destination != null && accountNumber == destination.AccountNumber) return destination;
                return null;
            });
    }

    // ===================================================================
    // GetPreviewAsync Tests
    // ===================================================================

    [Fact]
    public async Task GetPreviewAsync_ValidRequest_ReturnsPreview()
    {
        // Arrange
        var source = BuildActiveAccount(SourceAccountNumber, firstName: "Juan", lastName: "Perez");
        var destination = BuildActiveAccount(DestinationAccountNumber, firstName: "María", lastName: "López");
        SetupRepositories(source, destination);

        // Act
        var result = await _service.GetPreviewAsync(SourceAccountNumber, DestinationAccountNumber, 1000m);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Preview);
        Assert.Equal("Juan Perez", result.Preview!.SourceAccountOwner);
        Assert.Equal(SourceAccountNumber, result.Preview.SourceAccountNumber);
        Assert.Equal("María López", result.Preview.DestinationAccountOwner);
        Assert.Equal(DestinationAccountNumber, result.Preview.DestinationAccountNumber);
        Assert.Equal(1000m, result.Preview.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task GetPreviewAsync_InvalidAmount_ReturnsError(decimal amount)
    {
        // Arrange
        var source = BuildActiveAccount(SourceAccountNumber);
        var destination = BuildActiveAccount(DestinationAccountNumber);
        SetupRepositories(source, destination);

        // Act
        var result = await _service.GetPreviewAsync(SourceAccountNumber, DestinationAccountNumber, amount);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El monto de la transacción debe ser mayor que cero.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetPreviewAsync_SourceAccountNotFound_ReturnsError()
    {
        // Arrange
        var destination = BuildActiveAccount(DestinationAccountNumber);
        SetupRepositories(null, destination);

        // Act
        var result = await _service.GetPreviewAsync("999999999", DestinationAccountNumber, 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de cuenta origen ingresado no corresponde a una cuenta válida.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetPreviewAsync_InactiveSourceAccount_ReturnsError()
    {
        // Arrange
        var source = BuildActiveAccount(SourceAccountNumber);
        source.Status = AccountStatus.Cancelada;
        var destination = BuildActiveAccount(DestinationAccountNumber);
        SetupRepositories(source, destination);

        // Act
        var result = await _service.GetPreviewAsync(SourceAccountNumber, DestinationAccountNumber, 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de cuenta origen ingresado no corresponde a una cuenta válida.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetPreviewAsync_DestinationAccountNotFound_ReturnsError()
    {
        // Arrange
        var source = BuildActiveAccount(SourceAccountNumber);
        SetupRepositories(source, null);

        // Act
        var result = await _service.GetPreviewAsync(SourceAccountNumber, "999999999", 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de cuenta destino ingresado no corresponde a una cuenta válida.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetPreviewAsync_InactiveDestinationAccount_ReturnsError()
    {
        // Arrange
        var source = BuildActiveAccount(SourceAccountNumber);
        var destination = BuildActiveAccount(DestinationAccountNumber);
        destination.Status = AccountStatus.Cancelada;
        SetupRepositories(source, destination);

        // Act
        var result = await _service.GetPreviewAsync(SourceAccountNumber, DestinationAccountNumber, 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de cuenta destino ingresado no corresponde a una cuenta válida.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetPreviewAsync_SameAccount_ReturnsError()
    {
        // Arrange
        var source = BuildActiveAccount(SourceAccountNumber);
        SetupRepositories(source, source);

        // Act
        var result = await _service.GetPreviewAsync(SourceAccountNumber, SourceAccountNumber, 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("La cuenta origen y la cuenta destino no pueden ser la misma.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetPreviewAsync_InsufficientBalance_ReturnsError()
    {
        // Arrange
        var source = BuildActiveAccount(SourceAccountNumber, balance: 100m);
        var destination = BuildActiveAccount(DestinationAccountNumber);
        SetupRepositories(source, destination);

        // Act
        var result = await _service.GetPreviewAsync(SourceAccountNumber, DestinationAccountNumber, 500m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El monto ingresado excede el saldo disponible de la cuenta.", result.Error);
        Assert.Null(result.Preview);
    }

    // ===================================================================
    // CreateTransactionAsync Tests
    // ===================================================================

    [Fact]
    public async Task CreateTransactionAsync_ValidTransaction_DebitsSourceCreditsDestinationAndRecordsCrossedEntries()
    {
        // Arrange
        var source = BuildActiveAccount(SourceAccountNumber, balance: 5000m);
        var destination = BuildActiveAccount(DestinationAccountNumber, balance: 1000m);
        SetupRepositories(source, destination);

        // Act
        var result = await _service.CreateTransactionAsync(_tellerId, BuildDto(1000m));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(4000m, source.Balance);
        Assert.Equal(2000m, destination.Balance);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);

        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Type == TransactionType.DÉBITO &&
            t.Status == TransactionStatus.APROBADA &&
            t.SavingsAccountId == source.Id &&
            t.Origin == SourceAccountNumber &&
            t.Beneficiary == DestinationAccountNumber &&
            t.Amount == 1000m &&
            t.PerformedById == _tellerId)), Times.Once);

        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Type == TransactionType.CRÉDITO &&
            t.Status == TransactionStatus.APROBADA &&
            t.SavingsAccountId == destination.Id &&
            t.Origin == SourceAccountNumber &&
            t.Beneficiary == DestinationAccountNumber &&
            t.Amount == 1000m &&
            t.PerformedById == _tellerId)), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionAsync_DestinationNotFound_RecordsRejectedOnSourceWithoutModifyingBalances()
    {
        // Arrange
        var source = BuildActiveAccount(SourceAccountNumber, balance: 5000m);
        SetupRepositories(source, null);

        // Act
        var result = await _service.CreateTransactionAsync(_tellerId, BuildDto(1000m, destination: "999999999"));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de cuenta destino ingresado no corresponde a una cuenta válida.", result.Error);
        Assert.Equal(5000m, source.Balance);
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Status == TransactionStatus.RECHAZADA &&
            t.Type == TransactionType.DÉBITO &&
            t.SavingsAccountId == source.Id &&
            t.Beneficiary == "999999999" &&
            t.PerformedById == _tellerId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateTransactionAsync_SameAccount_RecordsRejectedTransaction()
    {
        // Arrange
        var source = BuildActiveAccount(SourceAccountNumber, balance: 5000m);
        SetupRepositories(source, source);

        // Act
        var result = await _service.CreateTransactionAsync(_tellerId, BuildDto(1000m, destination: SourceAccountNumber));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("La cuenta origen y la cuenta destino no pueden ser la misma.", result.Error);
        Assert.Equal(5000m, source.Balance);
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Status == TransactionStatus.RECHAZADA &&
            t.Type == TransactionType.DÉBITO)), Times.Once);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateTransactionAsync_InsufficientBalance_RecordsRejectedTransactionWithoutModifyingBalances()
    {
        // Arrange
        var source = BuildActiveAccount(SourceAccountNumber, balance: 100m);
        var destination = BuildActiveAccount(DestinationAccountNumber, balance: 1000m);
        SetupRepositories(source, destination);

        // Act
        var result = await _service.CreateTransactionAsync(_tellerId, BuildDto(500m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El monto ingresado excede el saldo disponible de la cuenta.", result.Error);
        Assert.Equal(100m, source.Balance);
        Assert.Equal(1000m, destination.Balance);
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Status == TransactionStatus.RECHAZADA &&
            t.Type == TransactionType.DÉBITO &&
            t.Amount == 500m &&
            t.PerformedById == _tellerId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateTransactionAsync_SourceNotFound_ReturnsErrorWithoutRecordingTransaction()
    {
        // Arrange
        var destination = BuildActiveAccount(DestinationAccountNumber);
        SetupRepositories(null, destination);

        // Act
        var result = await _service.CreateTransactionAsync(_tellerId, BuildDto(100m, source: "999999999"));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de cuenta origen ingresado no corresponde a una cuenta válida.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateTransactionAsync_NonPositiveAmount_ReturnsErrorWithoutPersisting()
    {
        // Arrange
        var source = BuildActiveAccount(SourceAccountNumber);
        var destination = BuildActiveAccount(DestinationAccountNumber);
        SetupRepositories(source, destination);

        // Act
        var result = await _service.CreateTransactionAsync(_tellerId, BuildDto(0m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El monto de la transacción debe ser mayor que cero.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    // ===================================================================
    // Email Notification Tests
    // ===================================================================

    [Fact]
    public async Task CreateTransactionAsync_DifferentClients_SendsTwoEmails()
    {
        // Arrange
        var source = BuildActiveAccount(SourceAccountNumber, balance: 5000m, firstName: "Juan", lastName: "Perez");
        var destination = BuildActiveAccount(DestinationAccountNumber, firstName: "María", lastName: "López");
        SetupRepositories(source, destination);

        // Act
        var result = await _service.CreateTransactionAsync(_tellerId, BuildDto(500m));

        // Assert
        Assert.True(result.Success);
        _emailService.Verify(e => e.SendAsync(
            source.Client!.Email!,
            It.Is<string>(s => s.Contains($"Transacción realizada a la cuenta {DestinationAccountNumber.Substring(5)}")),
            It.IsAny<string>()), Times.Once);
        _emailService.Verify(e => e.SendAsync(
            destination.Client!.Email!,
            It.Is<string>(s => s.Contains($"Transacción enviada desde la cuenta {SourceAccountNumber.Substring(5)}")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionAsync_SameClientOwnsBothAccounts_SendsBothNotifications()
    {
        // Arrange
        var sharedClientId = Guid.NewGuid();
        var source = BuildActiveAccount(SourceAccountNumber, balance: 5000m, clientId: sharedClientId);
        var destination = BuildActiveAccount(DestinationAccountNumber, clientId: sharedClientId);
        SetupRepositories(source, destination);

        // Act
        var result = await _service.CreateTransactionAsync(_tellerId, BuildDto(500m));

        // Assert
        Assert.True(result.Success);
        _emailService.Verify(e => e.SendAsync(
            source.Client!.Email!,
            It.Is<string>(s => s.Contains("Transacción realizada a la cuenta")),
            It.IsAny<string>()), Times.Once);
        _emailService.Verify(e => e.SendAsync(
            destination.Client!.Email!,
            It.Is<string>(s => s.Contains("Transacción enviada desde la cuenta")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionAsync_EmailFailure_SucceedsButReportsEmailNotSent()
    {
        // Arrange
        var source = BuildActiveAccount(SourceAccountNumber, balance: 5000m);
        var destination = BuildActiveAccount(DestinationAccountNumber, balance: 0m);
        SetupRepositories(source, destination);
        _emailService
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP down"));

        // Act
        var result = await _service.CreateTransactionAsync(_tellerId, BuildDto(500m));

        // Assert
        Assert.True(result.Success);
        Assert.False(result.EmailSent);
        Assert.Equal(4500m, source.Balance);
        Assert.Equal(500m, destination.Balance);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
    }
}
