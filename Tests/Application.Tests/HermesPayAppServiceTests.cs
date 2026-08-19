using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Services.Banking;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;
using Shared.Interfaces;
using Xunit;

namespace Application.Tests;

public class HermesPayAppServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IMerchantRepository> _merchants;
    private readonly Mock<ISavingsAccountRepository> _savingsAccounts;
    private readonly Mock<ICreditCardRepository> _creditCards;
    private readonly Mock<ICreditCardTransactionRepository> _creditCardTransactions;
    private readonly Mock<ITransactionRepository> _transactions;
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<IEmailService> _emailService;
    private readonly HermesPayAppService _service;

    public HermesPayAppServiceTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _merchants = new Mock<IMerchantRepository>();
        _savingsAccounts = new Mock<ISavingsAccountRepository>();
        _creditCards = new Mock<ICreditCardRepository>();
        _creditCardTransactions = new Mock<ICreditCardTransactionRepository>();
        _transactions = new Mock<ITransactionRepository>();

        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);
        _emailService = new Mock<IEmailService>();

        _unitOfWork.SetupGet(u => u.Merchants).Returns(_merchants.Object);
        _unitOfWork.SetupGet(u => u.SavingsAccounts).Returns(_savingsAccounts.Object);
        _unitOfWork.SetupGet(u => u.CreditCards).Returns(_creditCards.Object);
        _unitOfWork.SetupGet(u => u.CreditCardTransactions).Returns(_creditCardTransactions.Object);
        _unitOfWork.SetupGet(u => u.Transactions).Returns(_transactions.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _service = new HermesPayAppService(_unitOfWork.Object, _userManager.Object, _emailService.Object);
    }

    [Fact]
    public async Task ProcessPayment_SufficientCredit_ApprovesPaymentAndUpdatesBalances()
    {
        var commerceId = Guid.NewGuid();
        var merchantUser = new ApplicationUser { Id = Guid.NewGuid(), Email = "merchant@test.com" };
        var merchant = new Merchant
        {
            Id = commerceId,
            Name = "Tienda ABC",
            Email = "tienda@abc.com",
            Status = MerchantStatus.Activo,
            Users = new List<ApplicationUser> { merchantUser }
        };
        var merchantAccount = new SavingsAccount
        {
            Id = Guid.NewGuid(),
            AccountNumber = "111222333",
            Balance = 1000m,
            Status = AccountStatus.Activa
        };

        var cvcHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("789")));
        var card = new CreditCard
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            CardNumber = "1234567890123456",
            Limit = 10000m,
            Debt = 2000m,
            ExpirationDate = "12/28",
            CvcHash = cvcHash,
            Status = CardStatus.Activa
        };

        _merchants.Setup(m => m.GetByIdWithUsersAsync(commerceId)).ReturnsAsync(merchant);
        _savingsAccounts.Setup(s => s.GetPrimaryByClientIdAsync(merchantUser.Id)).ReturnsAsync(merchantAccount);
        _creditCards.Setup(c => c.GetByCardNumberAsync("1234567890123456")).ReturnsAsync(card);
        _userManager.Setup(u => u.FindByIdAsync(card.ClientId.ToString())).ReturnsAsync(new ApplicationUser { FirstName = "Cliente", Email = "cliente@test.com" });

        var dto = new ProcessPaymentDto
        {
            CardNumber = "1234567890123456",
            MonthExpirationCard = "12",
            YearExpirationCard = "2028",
            Cvc = "789",
            TransactionAmount = 1500m
        };

        var (success, errorCode, errorMessage) = await _service.ProcessPaymentAsync(commerceId, dto);

        Assert.True(success);
        Assert.Null(errorCode);
        Assert.Equal(3500m, card.Debt);
        Assert.Equal(2500m, merchantAccount.Balance);
        _creditCardTransactions.Verify(t => t.AddAsync(It.Is<CreditCardTransaction>(tx => tx.Status == CreditCardTransactionStatus.Aprobado && tx.Amount == 1500m)), Times.Once);
        _transactions.Verify(t => t.AddAsync(It.Is<Transaction>(tx => tx.Type == TransactionType.CRÉDITO && tx.Amount == 1500m)), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_ExceedsAvailableCredit_RecordsRejectedConsumptionAndReturnsBadRequest()
    {
        var commerceId = Guid.NewGuid();
        var merchantUser = new ApplicationUser { Id = Guid.NewGuid(), Email = "merchant@test.com" };
        var merchant = new Merchant
        {
            Id = commerceId,
            Name = "Tienda ABC",
            Status = MerchantStatus.Activo,
            Users = new List<ApplicationUser> { merchantUser }
        };
        var merchantAccount = new SavingsAccount
        {
            Id = Guid.NewGuid(),
            AccountNumber = "111222333",
            Balance = 1000m,
            Status = AccountStatus.Activa
        };

        var cvcHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("789")));
        var card = new CreditCard
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            CardNumber = "1234567890123456",
            Limit = 5000m,
            Debt = 4500m, // Available: 500
            ExpirationDate = "12/28",
            CvcHash = cvcHash,
            Status = CardStatus.Activa
        };

        _merchants.Setup(m => m.GetByIdWithUsersAsync(commerceId)).ReturnsAsync(merchant);
        _savingsAccounts.Setup(s => s.GetPrimaryByClientIdAsync(merchantUser.Id)).ReturnsAsync(merchantAccount);
        _creditCards.Setup(c => c.GetByCardNumberAsync("1234567890123456")).ReturnsAsync(card);

        var dto = new ProcessPaymentDto
        {
            CardNumber = "1234567890123456",
            MonthExpirationCard = "12",
            YearExpirationCard = "2028",
            Cvc = "789",
            TransactionAmount = 1000m // Exceeds 500
        };

        var (success, errorCode, errorMessage) = await _service.ProcessPaymentAsync(commerceId, dto);

        Assert.False(success);
        Assert.Equal("BadRequest", errorCode);
        Assert.Contains("excede el crédito disponible", errorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(4500m, card.Debt); // Not increased
        _creditCardTransactions.Verify(t => t.AddAsync(It.Is<CreditCardTransaction>(tx => tx.Status == CreditCardTransactionStatus.Rechazado && tx.Amount == 1000m)), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_InvalidCvc_ReturnsBadRequest()
    {
        var commerceId = Guid.NewGuid();
        var merchantUser = new ApplicationUser { Id = Guid.NewGuid(), Email = "merchant@test.com" };
        var merchant = new Merchant
        {
            Id = commerceId,
            Name = "Tienda ABC",
            Status = MerchantStatus.Activo,
            Users = new List<ApplicationUser> { merchantUser }
        };
        var merchantAccount = new SavingsAccount
        {
            Id = Guid.NewGuid(),
            AccountNumber = "111222333",
            Balance = 1000m,
            Status = AccountStatus.Activa
        };

        var cvcHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("789")));
        var card = new CreditCard
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            CardNumber = "1234567890123456",
            Limit = 5000m,
            Debt = 1000m,
            ExpirationDate = "12/28",
            CvcHash = cvcHash,
            Status = CardStatus.Activa
        };

        _merchants.Setup(m => m.GetByIdWithUsersAsync(commerceId)).ReturnsAsync(merchant);
        _savingsAccounts.Setup(s => s.GetPrimaryByClientIdAsync(merchantUser.Id)).ReturnsAsync(merchantAccount);
        _creditCards.Setup(c => c.GetByCardNumberAsync("1234567890123456")).ReturnsAsync(card);

        var dto = new ProcessPaymentDto
        {
            CardNumber = "1234567890123456",
            MonthExpirationCard = "12",
            YearExpirationCard = "2028",
            Cvc = "000", // Wrong CVC
            TransactionAmount = 500m
        };

        var (success, errorCode, errorMessage) = await _service.ProcessPaymentAsync(commerceId, dto);

        Assert.False(success);
        Assert.Equal("BadRequest", errorCode);
        Assert.Contains("CVC", errorMessage);
    }
}
