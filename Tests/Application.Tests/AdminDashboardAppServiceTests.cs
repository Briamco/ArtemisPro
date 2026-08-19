using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Application.Tests;

public class AdminDashboardAppServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<ICreditCardTransactionRepository> _cardTxRepoMock;
    private readonly Mock<ILoanInstallmentRepository> _installmentRepoMock;
    private readonly Mock<ISavingsAccountRepository> _savingsRepoMock;
    private readonly Mock<ILoanRepository> _loanRepoMock;
    private readonly Mock<ICreditCardRepository> _creditCardRepoMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly AdminDashboardAppService _dashboardService;

    public AdminDashboardAppServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _cardTxRepoMock = new Mock<ICreditCardTransactionRepository>();
        _installmentRepoMock = new Mock<ILoanInstallmentRepository>();
        _savingsRepoMock = new Mock<ISavingsAccountRepository>();
        _loanRepoMock = new Mock<ILoanRepository>();
        _creditCardRepoMock = new Mock<ICreditCardRepository>();

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _unitOfWorkMock.SetupGet(u => u.Transactions).Returns(_transactionRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.CreditCardTransactions).Returns(_cardTxRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.LoanInstallments).Returns(_installmentRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.SavingsAccounts).Returns(_savingsRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Loans).Returns(_loanRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.CreditCards).Returns(_creditCardRepoMock.Object);

        _dashboardService = new AdminDashboardAppService(_unitOfWorkMock.Object, _userManagerMock.Object);
    }

    [Fact]
    public async Task GetGeneralStatsAsync_CalculatesAllMetricsCorrectly()
    {
        var client1Id = Guid.NewGuid();
        var client2Id = Guid.NewGuid();
        var loan1Id = Guid.NewGuid();

        var clients = new List<ApplicationUser>
        {
            new() { Id = client1Id, IsActive = true },
            new() { Id = client2Id, IsActive = false }
        };

        var today = DateTime.Today;
        var transactions = new List<Transaction>
        {
            new() { Id = Guid.NewGuid(), Date = today.AddHours(2), Status = TransactionStatus.APROBADA },
            new() { Id = Guid.NewGuid(), Date = today.AddDays(-2), Status = TransactionStatus.APROBADA }
        };

        var cardTxs = new List<CreditCardTransaction>
        {
            new() { Id = Guid.NewGuid(), Date = today.AddHours(1), Status = CreditCardTransactionStatus.Aprobado },
            new() { Id = Guid.NewGuid(), Date = today.AddDays(-5), Status = CreditCardTransactionStatus.Aprobado }
        };

        var allInstallments = new List<LoanInstallment>
        {
            new() { Id = Guid.NewGuid(), DueDate = today, PaymentStatus = PaymentStatus.Pagada },
            new() { Id = Guid.NewGuid(), DueDate = today.AddDays(-10), PaymentStatus = PaymentStatus.Pagada },
            new() { Id = Guid.NewGuid(), LoanId = loan1Id, Amount = 8000m, PaymentStatus = PaymentStatus.Pendiente }
        };

        var activeAccounts = new List<SavingsAccount>
        {
            new() { Id = Guid.NewGuid(), Status = AccountStatus.Activa },
            new() { Id = Guid.NewGuid(), Status = AccountStatus.Activa }
        };

        var activeLoans = new List<Loan>
        {
            new() { Id = loan1Id, ClientId = client1Id, Status = LoanStatus.Activo }
        };

        var activeCards = new List<CreditCard>
        {
            new() { Id = Guid.NewGuid(), ClientId = client1Id, Status = CardStatus.Activa, Debt = 2000m }
        };

        _transactionRepoMock.Setup(t => t.GetAllAsync()).ReturnsAsync(transactions);
        _cardTxRepoMock.Setup(c => c.FindAsync(It.IsAny<Expression<Func<CreditCardTransaction, bool>>>())).ReturnsAsync(cardTxs);
        _installmentRepoMock.Setup(i => i.FindAsync(It.IsAny<Expression<Func<LoanInstallment, bool>>>()))
            .ReturnsAsync((Expression<Func<LoanInstallment, bool>> exp) =>
            {
                var compiled = exp.Compile();
                return allInstallments.Where(compiled).ToList();
            });

        _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Cliente")).ReturnsAsync(clients);
        _savingsRepoMock.Setup(s => s.FindAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>())).ReturnsAsync(activeAccounts);
        _loanRepoMock.Setup(l => l.FindAsync(It.IsAny<Expression<Func<Loan, bool>>>())).ReturnsAsync(activeLoans);
        _creditCardRepoMock.Setup(c => c.FindAsync(It.IsAny<Expression<Func<CreditCard, bool>>>())).ReturnsAsync(activeCards);

        var stats = await _dashboardService.GetGeneralStatsAsync();

        Assert.Equal(2, stats.TotalTransaccionesHistoricas);
        Assert.Equal(1, stats.TransaccionesDelDia);
        Assert.Equal(4, stats.TotalPagosHistoricos); // 2 cards + 2 loans
        Assert.Equal(2, stats.PagosDelDia); // 1 card today + 1 loan today
        Assert.Equal(1, stats.ClientesActivos);
        Assert.Equal(1, stats.ClientesInactivos);
        Assert.Equal(4, stats.TotalProductosFinancieros); // 2 accounts + 1 loan + 1 card
        Assert.Equal(2, stats.CuentasAhorroActivas);
        Assert.Equal(1, stats.PrestamosVigentes);
        Assert.Equal(1, stats.TarjetasCreditoActivas);
        // Average debt: (8000 loan + 2000 card) / 1 active client = 10000
        Assert.Equal(10000m, stats.MontoPromedioDeuda);
    }
}
