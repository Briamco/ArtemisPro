using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

public class LoanPaymentAppServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ISavingsAccountRepository> _savingsAccounts;
    private readonly Mock<ILoanRepository> _loans;
    private readonly Mock<ILoanInstallmentRepository> _loanInstallments;
    private readonly Mock<ITransactionRepository> _transactions;
    private readonly Mock<IEmailService> _emailService;
    private readonly LoanPaymentAppService _service;
    private readonly Guid _tellerId = Guid.NewGuid();

    public LoanPaymentAppServiceTests()
    {
        _savingsAccounts = new Mock<ISavingsAccountRepository>();
        _loans = new Mock<ILoanRepository>();
        _loanInstallments = new Mock<ILoanInstallmentRepository>();
        _transactions = new Mock<ITransactionRepository>();
        _emailService = new Mock<IEmailService>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _unitOfWork.SetupGet(u => u.SavingsAccounts).Returns(_savingsAccounts.Object);
        _unitOfWork.SetupGet(u => u.Loans).Returns(_loans.Object);
        _unitOfWork.SetupGet(u => u.LoanInstallments).Returns(_loanInstallments.Object);
        _unitOfWork.SetupGet(u => u.Transactions).Returns(_transactions.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _transactions
            .Setup(r => r.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => t);

        _service = new LoanPaymentAppService(
            _unitOfWork.Object,
            _emailService.Object,
            NullLogger<LoanPaymentAppService>.Instance);
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

    private static Loan BuildActiveLoan(string loanNumber = "111222333", Guid? clientId = null)
    {
        var cid = clientId ?? Guid.NewGuid();
        return new Loan
        {
            Id = Guid.NewGuid(),
            ClientId = cid,
            LoanNumber = loanNumber,
            ApprovedAmount = 12000m,
            Term = 12,
            AnnualInterestRate = 12m,
            Status = LoanStatus.Activo,
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

    private static LoanInstallment BuildInstallment(
        int number,
        DateTime dueDate,
        decimal amount,
        decimal pending,
        PaymentStatus status = PaymentStatus.Pendiente,
        bool isOverdue = false,
        Guid? loanId = null)
    {
        return new LoanInstallment
        {
            Id = Guid.NewGuid(),
            LoanId = loanId ?? Guid.NewGuid(),
            InstallmentNumber = number,
            DueDate = dueDate,
            Amount = amount,
            InterestAmount = amount * 0.1m,
            CapitalAmount = amount - (amount * 0.1m),
            PendingBalance = pending,
            PaymentStatus = status,
            IsOverdue = isOverdue
        };
    }

    private static CreateLoanPaymentDto BuildDto(string accountNumber = "123456789", string loanNumber = "111222333", decimal amount = 1000m)
    {
        return new CreateLoanPaymentDto
        {
            AccountNumber = accountNumber,
            LoanNumber = loanNumber,
            Amount = amount
        };
    }

    private void SetupRepositories(SavingsAccount? account, Loan? loan, List<LoanInstallment>? installments = null)
    {
        _savingsAccounts
            .Setup(r => r.GetByAccountNumberAsync(It.Is<string>(s => string.IsNullOrEmpty(s))))
            .ReturnsAsync((SavingsAccount?)null);
        _savingsAccounts
            .Setup(r => r.GetByAccountNumberAsync(It.Is<string>(s => !string.IsNullOrEmpty(s))))
            .ReturnsAsync(account);
        _loans
            .Setup(r => r.GetByLoanNumberAsync(It.IsAny<string>()))
            .ReturnsAsync(loan);
        _loanInstallments
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<LoanInstallment, bool>>>()))
            .ReturnsAsync((Expression<Func<LoanInstallment, bool>> predicate) =>
                (IEnumerable<LoanInstallment>)(installments ?? new List<LoanInstallment>())
                    .Where(predicate.Compile())
                    .ToList());
    }

    private static List<LoanInstallment> BuildThreePendingInstallments(Guid loanId)
    {
        return new List<LoanInstallment>
        {
            BuildInstallment(1, new DateTime(2026, 1, 1), 1000m, 1000m, loanId: loanId),
            BuildInstallment(2, new DateTime(2026, 2, 1), 1000m, 1000m, loanId: loanId),
            BuildInstallment(3, new DateTime(2026, 3, 1), 1000m, 1000m, loanId: loanId)
        };
    }

    // ===================================================================
    // GetLoanPaymentPreviewAsync Tests
    // ===================================================================

    [Fact]
    public async Task GetLoanPaymentPreviewAsync_ValidRequest_ReturnsPreview()
    {
        // Arrange
        var account = BuildActiveAccount();
        var loan = BuildActiveLoan();
        var installments = BuildThreePendingInstallments(loan.Id);
        SetupRepositories(account, loan, installments);

        // Act
        var result = await _service.GetLoanPaymentPreviewAsync(account.AccountNumber, loan.LoanNumber, 1000m);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Preview);
        Assert.Equal(account.AccountNumber, result.Preview!.OriginAccountNumber);
        Assert.Equal("Juan Perez", result.Preview.OriginAccountClientName);
        Assert.Equal(loan.LoanNumber, result.Preview.LoanNumber);
        Assert.Equal("María López", result.Preview.LoanClientName);
        Assert.Equal(1000m, result.Preview.EnteredAmount);
        Assert.Equal(1000m, result.Preview.EffectiveAmount);
    }

    [Fact]
    public async Task GetLoanPaymentPreviewAsync_AmountExceedsPending_CapsEffectiveAmountAtTotalPending()
    {
        // Arrange
        var account = BuildActiveAccount();
        var loan = BuildActiveLoan();
        SetupRepositories(account, loan, BuildThreePendingInstallments(loan.Id));

        // Act
        var result = await _service.GetLoanPaymentPreviewAsync(account.AccountNumber, loan.LoanNumber, 3000m);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3000m, result.Preview!.EnteredAmount);
        Assert.Equal(3000m, result.Preview.EffectiveAmount);
    }

    [Fact]
    public async Task GetLoanPaymentPreviewAsync_AmountExceedsPending_CapsAtSumOfPendingBalances()
    {
        // Arrange
        var account = BuildActiveAccount();
        var loan = BuildActiveLoan();
        var installments = BuildThreePendingInstallments(loan.Id);
        installments[0].PendingBalance = 1000m;
        installments[1].PendingBalance = 800m;
        installments[2].PendingBalance = 1000m;
        SetupRepositories(account, loan, installments);

        // Act
        var result = await _service.GetLoanPaymentPreviewAsync(account.AccountNumber, loan.LoanNumber, 5000m);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(5000m, result.Preview!.EnteredAmount);
        Assert.Equal(2800m, result.Preview.EffectiveAmount);
    }

    [Fact]
    public async Task GetLoanPaymentPreviewAsync_InsufficientBalance_ReturnsError()
    {
        // Arrange
        var account = BuildActiveAccount(balance: 100m);
        var loan = BuildActiveLoan();
        SetupRepositories(account, loan, BuildThreePendingInstallments(loan.Id));

        // Act
        var result = await _service.GetLoanPaymentPreviewAsync(account.AccountNumber, loan.LoanNumber, 500m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El monto ingresado excede el saldo disponible de la cuenta.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetLoanPaymentPreviewAsync_AccountNotFound_ReturnsError()
    {
        // Arrange
        var loan = BuildActiveLoan();
        SetupRepositories(null, loan, BuildThreePendingInstallments(loan.Id));

        // Act
        var result = await _service.GetLoanPaymentPreviewAsync("999999999", loan.LoanNumber, 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de cuenta ingresado no corresponde a una cuenta válida.", result.Error);
        Assert.Null(result.Preview);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetLoanPaymentPreviewAsync_NullOrEmptyAccountNumber_ReturnsError(string? accountNumber)
    {
        // Arrange
        var loan = BuildActiveLoan();
        SetupRepositories(BuildActiveAccount(), loan, BuildThreePendingInstallments(loan.Id));

        // Act
        var result = await _service.GetLoanPaymentPreviewAsync(accountNumber!, loan.LoanNumber, 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de cuenta ingresado no corresponde a una cuenta válida.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetLoanPaymentPreviewAsync_InactiveAccount_ReturnsError()
    {
        // Arrange
        var account = BuildActiveAccount();
        account.Status = AccountStatus.Cancelada;
        var loan = BuildActiveLoan();
        SetupRepositories(account, loan, BuildThreePendingInstallments(loan.Id));

        // Act
        var result = await _service.GetLoanPaymentPreviewAsync(account.AccountNumber, loan.LoanNumber, 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de cuenta ingresado no corresponde a una cuenta válida.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetLoanPaymentPreviewAsync_LoanNotFound_ReturnsError()
    {
        // Arrange
        var account = BuildActiveAccount();
        SetupRepositories(account, null);

        // Act
        var result = await _service.GetLoanPaymentPreviewAsync(account.AccountNumber, "111222333", 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de préstamo ingresado no corresponde a un préstamo válido.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetLoanPaymentPreviewAsync_InactiveLoan_ReturnsError()
    {
        // Arrange
        var account = BuildActiveAccount();
        var loan = BuildActiveLoan();
        loan.Status = LoanStatus.Completado;
        SetupRepositories(account, loan, BuildThreePendingInstallments(loan.Id));

        // Act
        var result = await _service.GetLoanPaymentPreviewAsync(account.AccountNumber, loan.LoanNumber, 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de préstamo ingresado no corresponde a un préstamo válido.", result.Error);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task GetLoanPaymentPreviewAsync_LoanNoPendingInstallments_ReturnsError()
    {
        // Arrange
        var account = BuildActiveAccount();
        var loan = BuildActiveLoan();
        var installments = new List<LoanInstallment>
        {
            BuildInstallment(1, new DateTime(2026, 1, 1), 1000m, 0m, PaymentStatus.Pagada, loanId: loan.Id),
            BuildInstallment(2, new DateTime(2026, 2, 1), 1000m, 0m, PaymentStatus.Pagada, loanId: loan.Id)
        };
        SetupRepositories(account, loan, installments);

        // Act
        var result = await _service.GetLoanPaymentPreviewAsync(account.AccountNumber, loan.LoanNumber, 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El préstamo seleccionado no tiene cuotas pendientes de pago.", result.Error);
        Assert.Null(result.Preview);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task GetLoanPaymentPreviewAsync_InvalidAmount_ReturnsError(decimal amount)
    {
        // Arrange
        var account = BuildActiveAccount();
        var loan = BuildActiveLoan();
        SetupRepositories(account, loan, BuildThreePendingInstallments(loan.Id));

        // Act
        var result = await _service.GetLoanPaymentPreviewAsync(account.AccountNumber, loan.LoanNumber, amount);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El monto a pagar debe ser mayor que cero.", result.Error);
        Assert.Null(result.Preview);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234")]
    [InlineData("1234567890")]
    public async Task GetLoanPaymentPreviewAsync_InvalidLoanNumberLength_ReturnsError(string loanNumber)
    {
        // Arrange
        var account = BuildActiveAccount();
        SetupRepositories(account, null);

        // Act
        var result = await _service.GetLoanPaymentPreviewAsync(account.AccountNumber, loanNumber, 1000m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número del préstamo debe contener 9 dígitos.", result.Error);
        Assert.Null(result.Preview);
    }

    // ===================================================================
    // CreateLoanPaymentAsync Tests
    // ===================================================================

    [Fact]
    public async Task CreateLoanPaymentAsync_ValidExactPayment_DebitsBalancePaysInstallmentAndApproves()
    {
        // Arrange
        var account = BuildActiveAccount(balance: 5000m);
        var loan = BuildActiveLoan();
        var installments = BuildThreePendingInstallments(loan.Id);
        SetupRepositories(account, loan, installments);

        // Act
        var result = await _service.CreateLoanPaymentAsync(_tellerId, BuildDto(account.AccountNumber, loan.LoanNumber, 1000m));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(4000m, account.Balance);
        Assert.Equal(PaymentStatus.Pagada, installments[0].PaymentStatus);
        Assert.Equal(0m, installments[0].PendingBalance);
        Assert.Equal(PaymentStatus.Pendiente, installments[1].PaymentStatus);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Type == TransactionType.DÉBITO &&
            t.Status == TransactionStatus.APROBADA &&
            t.Amount == 1000m &&
            t.Beneficiary == loan.LoanNumber &&
            t.PerformedById == _tellerId)), Times.Once);
    }

    [Fact]
    public async Task CreateLoanPaymentAsync_PaymentCoversMultipleInstallments_AppliesOldestFirst()
    {
        // Arrange
        var account = BuildActiveAccount(balance: 5000m);
        var loan = BuildActiveLoan();
        var installments = BuildThreePendingInstallments(loan.Id);
        SetupRepositories(account, loan, installments);

        // Act
        var result = await _service.CreateLoanPaymentAsync(_tellerId, BuildDto(account.AccountNumber, loan.LoanNumber, 1500m));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3500m, account.Balance);
        Assert.Equal(PaymentStatus.Pagada, installments[0].PaymentStatus);
        Assert.Equal(0m, installments[0].PendingBalance);
        Assert.Equal(PaymentStatus.Parcial, installments[1].PaymentStatus);
        Assert.Equal(500m, installments[1].PendingBalance);
        Assert.Equal(PaymentStatus.Pendiente, installments[2].PaymentStatus);
        Assert.Equal(1000m, installments[2].PendingBalance);
    }

    [Fact]
    public async Task CreateLoanPaymentAsync_PartialPayment_LeavesInstallmentAsParcial()
    {
        // Arrange
        var account = BuildActiveAccount(balance: 5000m);
        var loan = BuildActiveLoan();
        var installments = BuildThreePendingInstallments(loan.Id);
        SetupRepositories(account, loan, installments);

        // Act
        var result = await _service.CreateLoanPaymentAsync(_tellerId, BuildDto(account.AccountNumber, loan.LoanNumber, 400m));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(4600m, account.Balance);
        Assert.Equal(PaymentStatus.Parcial, installments[0].PaymentStatus);
        Assert.Equal(600m, installments[0].PendingBalance);
        Assert.Equal(PaymentStatus.Pendiente, installments[1].PaymentStatus);
        Assert.Equal(1000m, installments[1].PendingBalance);
        _loans.Verify(r => r.Update(It.IsAny<Loan>()), Times.Never);
    }

    [Fact]
    public async Task CreateLoanPaymentAsync_PaymentOfOverdueInstallment_ClearsIsOverdue()
    {
        // Arrange
        var account = BuildActiveAccount(balance: 5000m);
        var loan = BuildActiveLoan();
        var installments = new List<LoanInstallment>
        {
            BuildInstallment(1, new DateTime(2026, 1, 1), 1000m, 1000m, isOverdue: true, loanId: loan.Id),
            BuildInstallment(2, new DateTime(2026, 2, 1), 1000m, 1000m, loanId: loan.Id)
        };
        SetupRepositories(account, loan, installments);

        // Act
        var result = await _service.CreateLoanPaymentAsync(_tellerId, BuildDto(account.AccountNumber, loan.LoanNumber, 1000m));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Pagada, installments[0].PaymentStatus);
        Assert.False(installments[0].IsOverdue);
        Assert.Equal(PaymentStatus.Pendiente, installments[1].PaymentStatus);
    }

    [Fact]
    public async Task CreateLoanPaymentAsync_ExceedsPendingBalance_CapsAmountAndNeverOverpays()
    {
        // Arrange
        var account = BuildActiveAccount(balance: 5000m);
        var loan = BuildActiveLoan();
        var installments = BuildThreePendingInstallments(loan.Id);
        SetupRepositories(account, loan, installments);

        // Act
        var result = await _service.CreateLoanPaymentAsync(_tellerId, BuildDto(account.AccountNumber, loan.LoanNumber, 3500m));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2000m, account.Balance);
        Assert.All(installments, i => Assert.Equal(PaymentStatus.Pagada, i.PaymentStatus));
        Assert.All(installments, i => Assert.Equal(0m, i.PendingBalance));
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Amount == 3000m &&
            t.Status == TransactionStatus.APROBADA)), Times.Once);
    }

    [Fact]
    public async Task CreateLoanPaymentAsync_FullDebtPaid_MarksLoanAsCompletado()
    {
        // Arrange
        var account = BuildActiveAccount(balance: 5000m);
        var loan = BuildActiveLoan();
        var installments = BuildThreePendingInstallments(loan.Id);
        SetupRepositories(account, loan, installments);

        // Act
        var result = await _service.CreateLoanPaymentAsync(_tellerId, BuildDto(account.AccountNumber, loan.LoanNumber, 3000m));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(LoanStatus.Completado, loan.Status);
        _loans.Verify(r => r.Update(loan), Times.Once);
    }

    [Fact]
    public async Task CreateLoanPaymentAsync_InsufficientBalance_RecordsRejectedTransactionWithoutModifyingBalances()
    {
        // Arrange
        var account = BuildActiveAccount(balance: 100m);
        var loan = BuildActiveLoan();
        var installments = BuildThreePendingInstallments(loan.Id);
        SetupRepositories(account, loan, installments);

        // Act
        var result = await _service.CreateLoanPaymentAsync(_tellerId, BuildDto(account.AccountNumber, loan.LoanNumber, 500m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El monto ingresado excede el saldo disponible de la cuenta.", result.Error);
        Assert.Equal(100m, account.Balance);
        Assert.All(installments, i => Assert.Equal(PaymentStatus.Pendiente, i.PaymentStatus));
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Status == TransactionStatus.RECHAZADA &&
            t.Amount == 500m &&
            t.Beneficiary == loan.LoanNumber &&
            t.PerformedById == _tellerId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateLoanPaymentAsync_LoanNotFound_RecordsRejectedTransaction()
    {
        // Arrange
        var account = BuildActiveAccount();
        SetupRepositories(account, null);

        // Act
        var result = await _service.CreateLoanPaymentAsync(_tellerId, BuildDto(account.AccountNumber, "111222333", 100m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de préstamo ingresado no corresponde a un préstamo válido.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Status == TransactionStatus.RECHAZADA &&
            t.Beneficiary == "111222333")), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateLoanPaymentAsync_LoanNoPendingInstallments_RecordsRejectedTransaction()
    {
        // Arrange
        var account = BuildActiveAccount();
        var loan = BuildActiveLoan();
        var installments = new List<LoanInstallment>
        {
            BuildInstallment(1, new DateTime(2026, 1, 1), 1000m, 0m, PaymentStatus.Pagada, loanId: loan.Id)
        };
        SetupRepositories(account, loan, installments);

        // Act
        var result = await _service.CreateLoanPaymentAsync(_tellerId, BuildDto(account.AccountNumber, loan.LoanNumber, 100m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El préstamo seleccionado no tiene cuotas pendientes de pago.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Status == TransactionStatus.RECHAZADA)), Times.Once);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateLoanPaymentAsync_AccountNotFound_ReturnsErrorWithoutRecordingTransaction()
    {
        // Arrange
        var loan = BuildActiveLoan();
        SetupRepositories(null, loan, BuildThreePendingInstallments(loan.Id));

        // Act
        var result = await _service.CreateLoanPaymentAsync(_tellerId, BuildDto("000000000", loan.LoanNumber, 100m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número de cuenta ingresado no corresponde a una cuenta válida.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateLoanPaymentAsync_NonPositiveAmount_ReturnsErrorWithoutPersisting()
    {
        // Arrange
        var account = BuildActiveAccount();
        var loan = BuildActiveLoan();
        SetupRepositories(account, loan, BuildThreePendingInstallments(loan.Id));

        // Act
        var result = await _service.CreateLoanPaymentAsync(_tellerId, BuildDto(account.AccountNumber, loan.LoanNumber, 0m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El monto a pagar debe ser mayor que cero.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateLoanPaymentAsync_InvalidLoanNumberLength_ReturnsErrorWithoutPersisting()
    {
        // Arrange
        var account = BuildActiveAccount();
        SetupRepositories(account, null);

        // Act
        var result = await _service.CreateLoanPaymentAsync(_tellerId, BuildDto(account.AccountNumber, "1234", 100m));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El número del préstamo debe contener 9 dígitos.", result.Error);
        _transactions.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
    }

    // ===================================================================
    // Email Notification Tests
    // ===================================================================

    [Fact]
    public async Task CreateLoanPaymentAsync_SameClient_SendsOnlyLoanOwnerEmail()
    {
        // Arrange
        var sharedClientId = Guid.NewGuid();
        var account = BuildActiveAccount(balance: 5000m, clientId: sharedClientId);
        var loan = BuildActiveLoan(clientId: sharedClientId);
        SetupRepositories(account, loan, BuildThreePendingInstallments(loan.Id));

        // Act
        var result = await _service.CreateLoanPaymentAsync(_tellerId, BuildDto(account.AccountNumber, loan.LoanNumber, 500m));

        // Assert
        Assert.True(result.Success);
        _emailService.Verify(e => e.SendAsync(
            loan.Client!.Email!,
            It.Is<string>(s => s.Contains($"Pago realizado al préstamo {loan.LoanNumber}")),
            It.IsAny<string>()), Times.Once);
        _emailService.Verify(e => e.SendAsync(
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("Débito realizado")),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateLoanPaymentAsync_DifferentClients_SendsTwoEmails()
    {
        // Arrange
        var accountClientId = Guid.NewGuid();
        var loanClientId = Guid.NewGuid();
        var account = BuildActiveAccount(balance: 5000m, clientId: accountClientId);
        var loan = BuildActiveLoan(clientId: loanClientId);
        SetupRepositories(account, loan, BuildThreePendingInstallments(loan.Id));

        // Act
        var result = await _service.CreateLoanPaymentAsync(_tellerId, BuildDto(account.AccountNumber, loan.LoanNumber, 500m));

        // Assert
        Assert.True(result.Success);
        _emailService.Verify(e => e.SendAsync(
            loan.Client!.Email!,
            It.Is<string>(s => s.Contains($"Pago realizado al préstamo {loan.LoanNumber}")),
            It.IsAny<string>()), Times.Once);
        _emailService.Verify(e => e.SendAsync(
            account.Client!.Email!,
            It.Is<string>(s => s.Contains("Débito realizado desde su cuenta")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateLoanPaymentAsync_EmailFailure_SucceedsButReportsEmailNotSent()
    {
        // Arrange
        var account = BuildActiveAccount(balance: 5000m);
        var loan = BuildActiveLoan();
        var installments = BuildThreePendingInstallments(loan.Id);
        SetupRepositories(account, loan, installments);
        _emailService
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP down"));

        // Act
        var result = await _service.CreateLoanPaymentAsync(_tellerId, BuildDto(account.AccountNumber, loan.LoanNumber, 500m));

        // Assert
        Assert.True(result.Success);
        Assert.False(result.EmailSent);
        Assert.Equal(4500m, account.Balance);
        Assert.Equal(PaymentStatus.Parcial, installments[0].PaymentStatus);
        Assert.Equal(500m, installments[0].PendingBalance);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
    }
}
