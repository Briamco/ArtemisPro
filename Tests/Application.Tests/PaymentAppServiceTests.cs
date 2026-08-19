using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Moq;
using Shared.Interfaces;
using Xunit;

namespace Application.Tests;

public class PaymentAppServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ISavingsAccountRepository> _savingsAccountRepoMock;
    private readonly Mock<ICreditCardRepository> _creditCardRepoMock;
    private readonly Mock<ICreditCardTransactionRepository> _cardTransactionRepoMock;
    private readonly Mock<ILoanRepository> _loanRepoMock;
    private readonly Mock<ILoanInstallmentRepository> _installmentRepoMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<IApplicationUserRepository> _userRepoMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly PaymentAppService _paymentService;

    public PaymentAppServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _savingsAccountRepoMock = new Mock<ISavingsAccountRepository>();
        _creditCardRepoMock = new Mock<ICreditCardRepository>();
        _cardTransactionRepoMock = new Mock<ICreditCardTransactionRepository>();
        _loanRepoMock = new Mock<ILoanRepository>();
        _installmentRepoMock = new Mock<ILoanInstallmentRepository>();
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _userRepoMock = new Mock<IApplicationUserRepository>();
        _emailServiceMock = new Mock<IEmailService>();

        _unitOfWorkMock.SetupGet(u => u.SavingsAccounts).Returns(_savingsAccountRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.CreditCards).Returns(_creditCardRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.CreditCardTransactions).Returns(_cardTransactionRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Loans).Returns(_loanRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.LoanInstallments).Returns(_installmentRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Transactions).Returns(_transactionRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Users).Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _paymentService = new PaymentAppService(_unitOfWorkMock.Object, _emailServiceMock.Object);
    }

    [Fact]
    public async Task PayCreditCardAsync_CardWithoutDebt_ReturnsError()
    {
        var clientId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var account = new SavingsAccount { Id = accountId, ClientId = clientId, Status = AccountStatus.Activa, Balance = 5000m };
        var card = new CreditCard { Id = cardId, ClientId = clientId, Status = CardStatus.Activa, Debt = 0m };

        _savingsAccountRepoMock.Setup(s => s.GetByIdAsync(accountId)).ReturnsAsync(account);
        _creditCardRepoMock.Setup(c => c.GetByIdAsync(cardId)).ReturnsAsync(card);

        var dto = new PayCreditCardDto { ClientId = clientId, SourceAccountId = accountId, CreditCardId = cardId, Amount = 1000m };
        var (success, error) = await _paymentService.PayCreditCardAsync(dto);

        Assert.False(success);
        Assert.Equal("La tarjeta seleccionada no tiene deuda pendiente.", error);
    }

    [Fact]
    public async Task PayCreditCardAsync_Overpayment_CapsToRemainingDebt()
    {
        var clientId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var account = new SavingsAccount { Id = accountId, ClientId = clientId, Status = AccountStatus.Activa, Balance = 5000m, AccountNumber = "123456789" };
        var card = new CreditCard { Id = cardId, ClientId = clientId, Status = CardStatus.Activa, Debt = 1500m, CardNumber = "4000123456789010" };

        _savingsAccountRepoMock.Setup(s => s.GetByIdAsync(accountId)).ReturnsAsync(account);
        _creditCardRepoMock.Setup(c => c.GetByIdAsync(cardId)).ReturnsAsync(card);
        _userRepoMock.Setup(u => u.FindAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(new List<ApplicationUser> { new() { Id = clientId, Email = "client@bank.com", FirstName = "Juan", LastName = "Perez" } });

        var dto = new PayCreditCardDto { ClientId = clientId, SourceAccountId = accountId, CreditCardId = cardId, Amount = 3000m };
        var (success, error) = await _paymentService.PayCreditCardAsync(dto);

        Assert.True(success);
        Assert.Equal(0m, card.Debt);
        Assert.Equal(3500m, account.Balance);
        _transactionRepoMock.Verify(t => t.AddAsync(It.Is<Transaction>(tx => tx.Amount == 1500m && tx.Type == TransactionType.DÉBITO)), Times.Once);
    }

    [Fact]
    public async Task PayLoanAsync_ValidPayment_AppliesToPendingInstallmentsInOrder()
    {
        var clientId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var loanId = Guid.NewGuid();

        var account = new SavingsAccount { Id = accountId, ClientId = clientId, Status = AccountStatus.Activa, Balance = 10000m, AccountNumber = "123456789" };
        var loan = new Loan { Id = loanId, ClientId = clientId, Status = LoanStatus.Activo, LoanNumber = "987654321" };

        var inst1 = new LoanInstallment { Id = Guid.NewGuid(), LoanId = loanId, DueDate = DateTime.UtcNow.AddDays(-10), PendingBalance = 2000m, PaymentStatus = PaymentStatus.Pendiente };
        var inst2 = new LoanInstallment { Id = Guid.NewGuid(), LoanId = loanId, DueDate = DateTime.UtcNow.AddDays(20), PendingBalance = 2000m, PaymentStatus = PaymentStatus.Pendiente };

        _savingsAccountRepoMock.Setup(s => s.GetByIdAsync(accountId)).ReturnsAsync(account);
        _loanRepoMock.Setup(l => l.GetByIdAsync(loanId)).ReturnsAsync(loan);
        _installmentRepoMock.Setup(i => i.FindAsync(It.IsAny<Expression<Func<LoanInstallment, bool>>>()))
            .ReturnsAsync(new List<LoanInstallment> { inst1, inst2 });
        _userRepoMock.Setup(u => u.FindAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(new List<ApplicationUser> { new() { Id = clientId, Email = "client@bank.com", FirstName = "Maria" } });

        var dto = new PayLoanDto { ClientId = clientId, SourceAccountId = accountId, LoanId = loanId, Amount = 3000m };
        var (success, error) = await _paymentService.PayLoanAsync(dto);

        Assert.True(success);
        Assert.Equal(0m, inst1.PendingBalance);
        Assert.Equal(PaymentStatus.Pagada, inst1.PaymentStatus);
        Assert.Equal(1000m, inst2.PendingBalance);
        Assert.Equal(PaymentStatus.Parcial, inst2.PaymentStatus);
        Assert.Equal(7000m, account.Balance);
    }

    [Fact]
    public async Task CashAdvanceAsync_ExceedsAvailableCreditWithFee_RejectsAndRecordsTransaction()
    {
        var clientId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var account = new SavingsAccount { Id = accountId, ClientId = clientId, Status = AccountStatus.Activa, Balance = 1000m, AccountNumber = "123456789" };
        var card = new CreditCard { Id = cardId, ClientId = clientId, Status = CardStatus.Activa, Limit = 10000m, Debt = 9500m, ExpirationDate = "12/29", CardNumber = "4000123456789010" };

        _creditCardRepoMock.Setup(c => c.GetByIdAsync(cardId)).ReturnsAsync(card);
        _savingsAccountRepoMock.Setup(s => s.GetByIdAsync(accountId)).ReturnsAsync(account);

        var dto = new CashAdvanceDto { ClientId = clientId, CreditCardId = cardId, DestinationAccountId = accountId, Amount = 500m };
        var (success, error) = await _paymentService.CashAdvanceAsync(dto);

        Assert.False(success);
        Assert.Equal("El avance solicitado excede el crédito disponible de la tarjeta seleccionada.", error);
        _cardTransactionRepoMock.Verify(t => t.AddAsync(It.Is<CreditCardTransaction>(tx => tx.Status == CreditCardTransactionStatus.Rechazado)), Times.Once);
    }

    [Fact]
    public async Task CashAdvanceAsync_Valid_AppliesSixPointTwentyFivePercentFeeAndDepositsToAccount()
    {
        var clientId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var account = new SavingsAccount { Id = accountId, ClientId = clientId, Status = AccountStatus.Activa, Balance = 200m, AccountNumber = "123456789" };
        var card = new CreditCard { Id = cardId, ClientId = clientId, Status = CardStatus.Activa, Limit = 20000m, Debt = 0m, ExpirationDate = "12/29", CardNumber = "4000123456789010" };

        _creditCardRepoMock.Setup(c => c.GetByIdAsync(cardId)).ReturnsAsync(card);
        _savingsAccountRepoMock.Setup(s => s.GetByIdAsync(accountId)).ReturnsAsync(account);
        _userRepoMock.Setup(u => u.FindAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(new List<ApplicationUser> { new() { Id = clientId, Email = "client@bank.com", FirstName = "Ana" } });

        var dto = new CashAdvanceDto { ClientId = clientId, CreditCardId = cardId, DestinationAccountId = accountId, Amount = 1000m };
        var (success, error) = await _paymentService.CashAdvanceAsync(dto);

        Assert.True(success);
        Assert.Equal(1200m, account.Balance);
        // Fee = 62.50, Total charge = 1062.50
        Assert.Equal(1062.50m, card.Debt);
        _transactionRepoMock.Verify(t => t.AddAsync(It.Is<Transaction>(tx => tx.Type == TransactionType.CRÉDITO && tx.Amount == 1000m)), Times.Once);
        _cardTransactionRepoMock.Verify(t => t.AddAsync(It.Is<CreditCardTransaction>(tx => tx.Status == CreditCardTransactionStatus.Aprobado && tx.Amount == 1062.50m)), Times.Once);
    }
}
