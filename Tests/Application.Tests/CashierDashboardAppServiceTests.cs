using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Moq;
using Xunit;

namespace Application.Tests;

public class CashierDashboardAppServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly CashierDashboardAppService _dashboardService;

    public CashierDashboardAppServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _transactionRepoMock = new Mock<ITransactionRepository>();

        _unitOfWorkMock.SetupGet(u => u.Transactions).Returns(_transactionRepoMock.Object);

        _dashboardService = new CashierDashboardAppService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetTellerDailyStatsAsync_CalculatesMetricsForTellerToday()
    {
        var tellerId = Guid.NewGuid();
        var today = DateTime.Today;

        var transactions = new List<Transaction>
        {
            new() { Id = Guid.NewGuid(), PerformedById = tellerId, Origin = "DEPÓSITO", Beneficiary = "123456789", Status = TransactionStatus.APROBADA, Date = today.AddHours(2) },
            new() { Id = Guid.NewGuid(), PerformedById = tellerId, Origin = "123456789", Beneficiary = "RETIRO", Status = TransactionStatus.APROBADA, Date = today.AddHours(3) },
            new() { Id = Guid.NewGuid(), PerformedById = tellerId, Origin = "Pago de préstamo", Beneficiary = "987654321", Status = TransactionStatus.APROBADA, Date = today.AddHours(4) },
            new() { Id = Guid.NewGuid(), PerformedById = tellerId, Origin = "123456789", Beneficiary = "Pago de tarjeta", Status = TransactionStatus.APROBADA, Date = today.AddHours(5) },
            new() { Id = Guid.NewGuid(), PerformedById = tellerId, Origin = "DEPÓSITO", Beneficiary = "123456789", Status = TransactionStatus.RECHAZADA, Date = today.AddHours(6) },
            new() { Id = Guid.NewGuid(), PerformedById = tellerId, Origin = "DEPÓSITO", Beneficiary = "123456789", Status = TransactionStatus.APROBADA, Date = today.AddDays(-1) }
        };

        _transactionRepoMock.Setup(t => t.FindAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
            .ReturnsAsync(transactions);

        var stats = await _dashboardService.GetTellerDailyStatsAsync(tellerId);

        Assert.Equal(1, stats.TotalDepositsToday);
        Assert.Equal(1, stats.TotalWithdrawalsToday);
        Assert.Equal(2, stats.TotalPaymentsToday);
        Assert.Equal(4, stats.TotalTransactionsToday); // 4 approved today
    }
}
