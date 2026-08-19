using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Moq;
using Shared.Interfaces;
using Xunit;

namespace Application.Tests;

public class LoanAppServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILoanRepository> _loans;
    private readonly Mock<ILoanInstallmentRepository> _loanInstallments;
    private readonly Mock<ICreditCardRepository> _creditCards;
    private readonly Mock<ISavingsAccountRepository> _savingsAccounts;
    private readonly Mock<IApplicationUserRepository> _users;
    private readonly Mock<ITransactionRepository> _transactions;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<IEmailService> _emailService;
    private readonly LoanAppService _service;

    public LoanAppServiceTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _loans = new Mock<ILoanRepository>();
        _loanInstallments = new Mock<ILoanInstallmentRepository>();
        _creditCards = new Mock<ICreditCardRepository>();
        _savingsAccounts = new Mock<ISavingsAccountRepository>();
        _users = new Mock<IApplicationUserRepository>();
        _transactions = new Mock<ITransactionRepository>();
        _mapper = new Mock<IMapper>();
        _emailService = new Mock<IEmailService>();

        _unitOfWork.SetupGet(u => u.Loans).Returns(_loans.Object);
        _unitOfWork.SetupGet(u => u.LoanInstallments).Returns(_loanInstallments.Object);
        _unitOfWork.SetupGet(u => u.CreditCards).Returns(_creditCards.Object);
        _unitOfWork.SetupGet(u => u.SavingsAccounts).Returns(_savingsAccounts.Object);
        _unitOfWork.SetupGet(u => u.Users).Returns(_users.Object);
        _unitOfWork.SetupGet(u => u.Transactions).Returns(_transactions.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _service = new LoanAppService(_unitOfWork.Object, _mapper.Object, _emailService.Object);
    }

    [Fact]
    public async Task CreateLoan_ClientWithActiveAccount_DisbursesLoanAndCreatesInstallments()
    {
        var clientId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var client = new ApplicationUser { Id = clientId, IsActive = true, FirstName = "Carlos", LastName = "Santana", Email = "carlos@test.com" };
        var primaryAccount = new SavingsAccount
        {
            Id = Guid.NewGuid(),
            AccountNumber = "123456789",
            ClientId = clientId,
            Balance = 5000m,
            AccountType = AccountType.Principal,
            Status = AccountStatus.Activa
        };

        _users.Setup(u => u.GetByIdAsync(clientId))
            .ReturnsAsync(client);
        _savingsAccounts.Setup(s => s.GetPrimaryByClientIdAsync(clientId))
            .ReturnsAsync(primaryAccount);
        _savingsAccounts.Setup(s => s.ExistsAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
            .ReturnsAsync(false);
        _loans.Setup(l => l.ExistsAsync(It.IsAny<Expression<Func<Loan, bool>>>()))
            .ReturnsAsync(false);
        _unitOfWork.Setup(u => u.GetActiveClientsCountAsync()).ReturnsAsync(1);
        _loanInstallments.Setup(li => li.GetTotalPendingDebtByClientIdAsync(clientId)).ReturnsAsync(0m);
        _creditCards.Setup(cc => cc.GetTotalActiveDebtByClientIdAsync(clientId)).ReturnsAsync(0m);
        _loanInstallments.Setup(li => li.GetTotalSystemPendingDebtAsync()).ReturnsAsync(0m);
        _creditCards.Setup(cc => cc.GetTotalSystemActiveDebtAsync()).ReturnsAsync(0m);

        var dto = new CreateLoanDto
        {
            ClientId = clientId,
            CapitalAmount = 50000m,
            AnnualInterestRate = 18m,
            TermInMonths = 12,
            ConfirmHighRisk = true
        };

        var result = await _service.CreateLoanAsync(dto, adminId);

        Assert.NotNull(result);
        Assert.Equal(50000m, result.CapitalAmount);
        Assert.Equal(55000m, primaryAccount.Balance);
        _loans.Verify(l => l.AddAsync(It.Is<Loan>(loan => loan.ClientId == clientId && loan.ApprovedAmount == 50000m)), Times.Once);
        _transactions.Verify(t => t.AddAsync(It.Is<Transaction>(tx => tx.Amount == 50000m && tx.Type == TransactionType.CRÉDITO)), Times.Once);
    }

    [Fact]
    public async Task CreateLoan_ClientWithHighRiskWithoutConfirmation_ThrowsHighRiskConflictException()
    {
        var clientId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var client = new ApplicationUser { Id = clientId, IsActive = true, FirstName = "Carlos", LastName = "Santana", Email = "carlos@test.com" };
        var primaryAccount = new SavingsAccount
        {
            Id = Guid.NewGuid(),
            AccountNumber = "123456789",
            ClientId = clientId,
            Balance = 5000m,
            AccountType = AccountType.Principal,
            Status = AccountStatus.Activa
        };

        _users.Setup(u => u.GetByIdAsync(clientId))
            .ReturnsAsync(client);
        _savingsAccounts.Setup(s => s.GetPrimaryByClientIdAsync(clientId))
            .ReturnsAsync(primaryAccount);
        _unitOfWork.Setup(u => u.GetActiveClientsCountAsync()).ReturnsAsync(2);
        _loanInstallments.Setup(li => li.GetTotalPendingDebtByClientIdAsync(clientId)).ReturnsAsync(150000m);
        _creditCards.Setup(cc => cc.GetTotalActiveDebtByClientIdAsync(clientId)).ReturnsAsync(0m);
        // Average system debt: 50,000
        _loanInstallments.Setup(li => li.GetTotalSystemPendingDebtAsync()).ReturnsAsync(100000m);
        _creditCards.Setup(cc => cc.GetTotalSystemActiveDebtAsync()).ReturnsAsync(0m);

        var dto = new CreateLoanDto
        {
            ClientId = clientId,
            CapitalAmount = 50000m,
            AnnualInterestRate = 18m,
            TermInMonths = 12,
            ConfirmHighRisk = false
        };

        await Assert.ThrowsAsync<HighRiskConflictException>(() => _service.CreateLoanAsync(dto, adminId));
    }

    [Fact]
    public async Task UpdateLoanRate_ExistingLoan_UpdatesAnnualRate()
    {
        var loanId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var loan = new Loan
        {
            Id = loanId,
            ClientId = clientId,
            AnnualInterestRate = 15m,
            Status = LoanStatus.Activo
        };

        var installment = new LoanInstallment
        {
            Id = Guid.NewGuid(),
            LoanId = loanId,
            InstallmentNumber = 1,
            DueDate = DateTime.UtcNow.AddMonths(1),
            PaymentStatus = PaymentStatus.Pendiente,
            CapitalAmount = 10000m,
            InterestAmount = 100m,
            Amount = 10100m,
            PendingBalance = 10100m
        };

        _loans.Setup(l => l.GetByIdAsync(loanId)).ReturnsAsync(loan);
        _loanInstallments.Setup(li => li.FindAsync(It.IsAny<Expression<Func<LoanInstallment, bool>>>()))
            .ReturnsAsync(new List<LoanInstallment> { installment });
        _users.Setup(u => u.GetByIdAsync(clientId)).ReturnsAsync(new ApplicationUser { Email = "test@test.com" });

        var dto = new UpdateLoanRateDto { AnnualInterestRate = 22.5m };
        var (success, error) = await _service.UpdateLoanRateAsync(loanId, dto);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(22.5m, loan.AnnualInterestRate);
        _loans.Verify(l => l.Update(loan), Times.Once);
    }
}
