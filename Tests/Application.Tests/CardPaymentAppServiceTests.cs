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

public class CardPaymentAppServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ISavingsAccountRepository> _savingsAccounts;
    private readonly Mock<ICreditCardRepository> _creditCards;
    private readonly Mock<ITransactionRepository> _transactions;
    private readonly Mock<IEmailService> _emailService;
    private readonly CardPaymentAppService _service;
    private readonly Guid _tellerId = Guid.NewGuid();

    public CardPaymentAppServiceTests()
    {
        _savingsAccounts = new Mock<ISavingsAccountRepository>();
        _creditCards = new Mock<ICreditCardRepository>();
        _transactions = new Mock<ITransactionRepository>();
        _emailService = new Mock<IEmailService>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _unitOfWork.SetupGet(u => u.SavingsAccounts).Returns(_savingsAccounts.Object);
        _unitOfWork.SetupGet(u => u.CreditCards).Returns(_creditCards.Object);
        _unitOfWork.SetupGet(u => u.Transactions).Returns(_transactions.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _transactions
            .Setup(r => r.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => t);

        _service = new CardPaymentAppService(
            _unitOfWork.Object,
            _emailService.Object,
            NullLogger<CardPaymentAppService>.Instance);
    }

    // --- Helpers ---

    private static SavingsAccount BuildActiveAccount(decimal balance = 5000m, Guid? clientId = null)
    {
        var cid = clientId ?? Guid.NewGuid();
        return new SavingsAccount
        {
            Id = Guid.NewGuid(),
            ClientId = cid,
            AccountNumber = "123456789",
            Balance = balance,
            AccountType = AccountType.Principal,
            Status = AccountStatus.Activa,
            CreatedAt = DateTime.UtcNow,
            Client = new ApplicationUser
            {
                Id = cid,
                FirstName = "Juan",
                LastName = "Perez",
                Email = "juan.perez@example.com"
            }
        };
    }

    private static CreditCard BuildActiveCard(decimal debt = 3000m, Guid? clientId = null)
    {
        var cid = clientId ?? Guid.NewGuid();
        return new CreditCard
        {
            Id = Guid.NewGuid(),
            ClientId = cid,
            CardNumber = "4111111111111234",
            Limit = 10000m,
            Debt = debt,
            ExpirationDate = "12/30",
            CvcHash = "hashed",
            Status = CardStatus.Activa,
            AdminId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Client = new ApplicationUser
            {
                Id = cid,
                FirstName = "María",
                LastName = "López",
                Email = "maria.lopez@example.com"
            }
        };
    }

    private static CreateCardPaymentDto BuildDto(string accountNumber = "123456789", string cardNumber = "4111111111111234", decimal amount = 1000m)
    {
        return new CreateCardPaymentDto
        {
            AccountNumber = accountNumber,
            CardNumber = cardNumber,
            Amount = amount
        };
    }

    private void SetupRepositories(SavingsAccount? account, CreditCard? card)
    {
        _savingsAccounts
            .Setup(r => r.GetByAccountNumberAsync(It.IsAny<string>()))
            .ReturnsAsync(account);
        _creditCards
            .Setup(r => r.GetByCardNumberAsync(It.IsAny<string>()))
            .ReturnsAsync(card);
    }

    // ===================================================================
    // GetCardPaymentPreviewAsync Tests
    // ===================================================================

    [Fact]
    public async Task GetCardPaymentPreviewAsync_ValidRequest_ReturnsPreview()
    {
        // Arrange
        var account = BuildActiveAccount();
        var card = BuildActiveCard(debt: 3000m);
        SetupRepositories(account, card);

        // Act
        var result = await _service.GetCardPaymentPreviewAsync(account.AccountNumber, card.CardNumber, 1000m);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Preview);
        Assert.Equal(account.AccountNumber, result.Preview!.OriginAccountNumber);
        Assert.Equal("Juan Perez", result.Preview.OriginAccountClientName);
        Assert.Equal("1234", result.Preview.CardLast4);
        Assert.Equal("María López", result.Preview.CardClientName);
        Assert.Equal(1000m, result.Preview.EnteredAmount);
        Assert.Equal(1000m, result.Preview.EffectiveAmount);
    }

    [Fact]
    public async Task GetCardPaymentPreviewAsync_AmountExceedsDebt_CapsEffectiveAmountAtDebt()
    {
        // Arrange
        var account = BuildActiveAccount();
        var card = BuildActiveCard(debt: 500m);
        SetupRepositories(account, card);

        // Act
        var result = await _service.GetCardPaymentPreviewAsync(account.AccountNumber, card.CardNumber, 1000m);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1000m, result.Preview!.EnteredAmount);
        Assert.Equal(500m, result.Preview.EffectiveAmount);
    }

    [Fact]
    public async Task GetCardPaymentPreviewAsync_InsufficientBalance_ReturnsError()
    {
        // Arrange
        var account = BuildActiveAccount(balance: 100m);
        var card = BuildActiveCard(debt: 3000m);
        SetupRepositories(account, card);

        // Act
        var result = await _service.GetCardPaymentPreviewAsync(account.AccountNumber, card.CardNumber, 500m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El monto ingresado excede el saldo disponible de la cuenta.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetCardPaymentPreviewAsync_AccountNotFound_ReturnsError()
    {
        // Arrange
        var card = BuildActiveCard();
        SetupRepositories(null, card);

        // Act
        var result = await _service.GetCardPaymentPreviewAsync("999999999", card.CardNumber, 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de cuenta ingresado no corresponde a una cuenta válida.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetCardPaymentPreviewAsync_InactiveAccount_ReturnsError()
    {
        // Arrange
        var account = BuildActiveAccount();
        account.Status = AccountStatus.Cancelada;
        var card = BuildActiveCard();
        SetupRepositories(account, card);

        // Act
        var result = await _service.GetCardPaymentPreviewAsync(account.AccountNumber, card.CardNumber, 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de cuenta ingresado no corresponde a una cuenta válida.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetCardPaymentPreviewAsync_CardNotFound_ReturnsError()
    {
        // Arrange
        var account = BuildActiveAccount();
        SetupRepositories(account, null);

        // Act
        var result = await _service.GetCardPaymentPreviewAsync(account.AccountNumber, "4111111111119999", 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de tarjeta ingresado no corresponde a una tarjeta válida.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetCardPaymentPreviewAsync_InactiveCard_ReturnsError()
    {
        // Arrange
        var account = BuildActiveAccount();
        var card = BuildActiveCard();
        card.Status = CardStatus.Cancelada;
        SetupRepositories(account, card);

        // Act
        var result = await _service.GetCardPaymentPreviewAsync(account.AccountNumber, card.CardNumber, 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de tarjeta ingresado no corresponde a una tarjeta válida.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetCardPaymentPreviewAsync_CardNoDebt_ReturnsError()
    {
        // Arrange
        var account = BuildActiveAccount();
        var card = BuildActiveCard(debt: 0m);
        SetupRepositories(account, card);

        // Act
        var result = await _service.GetCardPaymentPreviewAsync(account.AccountNumber, card.CardNumber, 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("La tarjeta seleccionada no tiene deuda pendiente.", result.Error);
        Assert.Null(result.Preview);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task GetCardPaymentPreviewAsync_InvalidAmount_ReturnsError(decimal amount)
    {
        // Arrange
        var account = BuildActiveAccount();
        var card = BuildActiveCard();
        SetupRepositories(account, card);

        // Act
        var result = await _service.GetCardPaymentPreviewAsync(account.AccountNumber, card.CardNumber, amount);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El monto a pagar debe ser mayor que cero.", result.Error);
        Assert.Null(result.Preview);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234")]
    [InlineData("12345678901234567")]
    public async Task GetCardPaymentPreviewAsync_InvalidCardNumberLength_ReturnsError(string cardNumber)
    {
        // Arrange
        var account = BuildActiveAccount();
        SetupRepositories(account, null);

        // Act
        var result = await _service.GetCardPaymentPreviewAsync(account.AccountNumber, cardNumber, 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de tarjeta debe contener 16 dígitos.", result.Error);
        Assert.Null(result.Preview);
    }

    // ===================================================================
    // CreateCardPaymentAsync Tests
    // ===================================================================

    [Fact]
    public async Task CreateCardPaymentAsync_ValidPaymentExactAmount_DebitsBalanceReducesDebtAndApproves()
    {
        // Arrange
        var account = BuildActiveAccount(balance: 5000m);
        var card = BuildActiveCard(debt: 1000m);
        SetupRepositories(account, card);

        // Act
        var result = await _service.CreateCardPaymentAsync(_tellerId, BuildDto(account.AccountNumber, card.CardNumber, 1000m));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(4000m, account.Balance);
        Assert.Equal(0m, card.Debt);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Type == TransactionType.DÉBITO &&
            t.Status == TransactionStatus.APROBADA &&
            t.Amount == 1000m &&
            t.PerformedById == _tellerId)), Times.Once);
    }

    [Fact]
    public async Task CreateCardPaymentAsync_AmountExceedsDebt_CapsPaymentAtDebt()
    {
        // Arrange
        var account = BuildActiveAccount(balance: 5000m);
        var card = BuildActiveCard(debt: 500m);
        SetupRepositories(account, card);

        // Act
        var result = await _service.CreateCardPaymentAsync(_tellerId, BuildDto(account.AccountNumber, card.CardNumber, 2000m));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(4500m, account.Balance);
        Assert.Equal(0m, card.Debt);
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Amount == 500m &&
            t.Status == TransactionStatus.APROBADA)), Times.Once);
    }

    [Fact]
    public async Task CreateCardPaymentAsync_InsufficientBalance_RecordsRejectedTransactionWithoutModifyingBalances()
    {
        // Arrange
        var account = BuildActiveAccount(balance: 100m);
        var card = BuildActiveCard(debt: 3000m);
        SetupRepositories(account, card);

        // Act
        var result = await _service.CreateCardPaymentAsync(_tellerId, BuildDto(account.AccountNumber, card.CardNumber, 500m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El monto ingresado excede el saldo disponible de la cuenta.", result.Error);
        Assert.Equal(100m, account.Balance);
        Assert.Equal(3000m, card.Debt);
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Status == TransactionStatus.RECHAZADA &&
            t.Amount == 500m &&
            t.PerformedById == _tellerId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateCardPaymentAsync_AccountNotFound_ReturnsErrorWithoutRecordingTransaction()
    {
        // Arrange
        SetupRepositories(null, BuildActiveCard());

        // Act
        var result = await _service.CreateCardPaymentAsync(_tellerId, BuildDto("000000000", "4111111111111234", 100m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de cuenta ingresado no corresponde a una cuenta válida.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateCardPaymentAsync_InactiveAccount_ReturnsErrorWithoutRecordingTransaction()
    {
        // Arrange
        var account = BuildActiveAccount();
        account.Status = AccountStatus.Cancelada;
        SetupRepositories(account, BuildActiveCard());

        // Act
        var result = await _service.CreateCardPaymentAsync(_tellerId, BuildDto(account.AccountNumber, "4111111111111234", 100m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de cuenta ingresado no corresponde a una cuenta válida.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateCardPaymentAsync_CardNotFound_RecordsRejectedTransaction()
    {
        // Arrange
        var account = BuildActiveAccount();
        SetupRepositories(account, null);

        // Act
        var result = await _service.CreateCardPaymentAsync(_tellerId, BuildDto(account.AccountNumber, "4111111111119999", 100m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de tarjeta ingresado no corresponde a una tarjeta válida.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Status == TransactionStatus.RECHAZADA)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateCardPaymentAsync_InactiveCard_RecordsRejectedTransaction()
    {
        // Arrange
        var account = BuildActiveAccount();
        var card = BuildActiveCard();
        card.Status = CardStatus.Cancelada;
        SetupRepositories(account, card);

        // Act
        var result = await _service.CreateCardPaymentAsync(_tellerId, BuildDto(account.AccountNumber, card.CardNumber, 100m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de tarjeta ingresado no corresponde a una tarjeta válida.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Status == TransactionStatus.RECHAZADA)), Times.Once);
    }

    [Fact]
    public async Task CreateCardPaymentAsync_CardNoDebt_RecordsRejectedTransaction()
    {
        // Arrange
        var account = BuildActiveAccount();
        var card = BuildActiveCard(debt: 0m);
        SetupRepositories(account, card);

        // Act
        var result = await _service.CreateCardPaymentAsync(_tellerId, BuildDto(account.AccountNumber, card.CardNumber, 100m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("La tarjeta seleccionada no tiene deuda pendiente.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Status == TransactionStatus.RECHAZADA)), Times.Once);
    }

    [Fact]
    public async Task CreateCardPaymentAsync_NonPositiveAmount_ReturnsErrorWithoutPersisting()
    {
        // Arrange
        var account = BuildActiveAccount();
        var card = BuildActiveCard();
        SetupRepositories(account, card);

        // Act
        var result = await _service.CreateCardPaymentAsync(_tellerId, BuildDto(account.AccountNumber, card.CardNumber, 0m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El monto a pagar debe ser mayor que cero.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateCardPaymentAsync_InvalidCardNumberLength_ReturnsErrorWithoutPersisting()
    {
        // Arrange
        var account = BuildActiveAccount();
        SetupRepositories(account, null);

        // Act
        var result = await _service.CreateCardPaymentAsync(_tellerId, BuildDto(account.AccountNumber, "1234", 100m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de tarjeta debe contener 16 dígitos.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
    }

    // ===================================================================
    // Email Notification Tests
    // ===================================================================

    [Fact]
    public async Task CreateCardPaymentAsync_SameClient_SendsOnlyCardOwnerEmail()
    {
        // Arrange
        var sharedClientId = Guid.NewGuid();
        var account = BuildActiveAccount(balance: 5000m, clientId: sharedClientId);
        var card = BuildActiveCard(debt: 1000m, clientId: sharedClientId);
        SetupRepositories(account, card);

        // Act
        var result = await _service.CreateCardPaymentAsync(_tellerId, BuildDto(account.AccountNumber, card.CardNumber, 500m));

        // Assert
        Assert.True(result.Success);
        // Only one email to card owner; no second email since same client owns both
        _emailService.Verify(e => e.SendAsync(
            card.Client!.Email!,
            It.Is<string>(s => s.Contains("Pago realizado")),
            It.IsAny<string>()), Times.Once);
        _emailService.Verify(e => e.SendAsync(
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("Débito realizado")),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateCardPaymentAsync_DifferentClients_SendsTwoEmails()
    {
        // Arrange
        var accountClientId = Guid.NewGuid();
        var cardClientId = Guid.NewGuid();
        var account = BuildActiveAccount(balance: 5000m, clientId: accountClientId);
        var card = BuildActiveCard(debt: 1000m, clientId: cardClientId);
        SetupRepositories(account, card);

        // Act
        var result = await _service.CreateCardPaymentAsync(_tellerId, BuildDto(account.AccountNumber, card.CardNumber, 500m));

        // Assert
        Assert.True(result.Success);
        // Email to card owner
        _emailService.Verify(e => e.SendAsync(
            card.Client!.Email!,
            It.Is<string>(s => s.Contains("Pago realizado")),
            It.IsAny<string>()), Times.Once);
        // Email to account owner (different client)
        _emailService.Verify(e => e.SendAsync(
            account.Client!.Email!,
            It.Is<string>(s => s.Contains("Débito realizado")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateCardPaymentAsync_EmailFailure_SucceedsButReportsEmailNotSent()
    {
        // Arrange
        var account = BuildActiveAccount(balance: 5000m);
        var card = BuildActiveCard(debt: 1000m);
        SetupRepositories(account, card);
        _emailService
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP down"));

        // Act
        var result = await _service.CreateCardPaymentAsync(_tellerId, BuildDto(account.AccountNumber, card.CardNumber, 500m));

        // Assert
        Assert.True(result.Success);
        Assert.False(result.EmailSent);
        Assert.Equal(4500m, account.Balance);
        Assert.Equal(500m, card.Debt);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
    }
}
