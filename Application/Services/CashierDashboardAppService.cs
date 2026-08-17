using System;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Enums;

namespace Application.Services;

public class CashierDashboardAppService : ICashierDashboardAppService
{
    private readonly IUnitOfWork _unitOfWork;

    public CashierDashboardAppService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CashierDashboardStatsDto> GetTellerDailyStatsAsync(Guid tellerId)
    {
        var today = DateTime.Today;
        var allTransactions = await _unitOfWork.Transactions.FindAsync(t => t.PerformedById == tellerId);
        var todayTransactions = allTransactions.Where(t => t.Date.Date == today).ToList();

        return new CashierDashboardStatsDto
        {
            TotalDepositsToday = todayTransactions.Count(t => t.Origin == "DEPÓSITO" && t.Status == TransactionStatus.APROBADA),
            TotalWithdrawalsToday = todayTransactions.Count(t => t.Beneficiary == "RETIRO" && t.Status == TransactionStatus.APROBADA),
            TotalPaymentsToday = todayTransactions.Count(t => (t.Origin == "Pago de préstamo" || t.Beneficiary == "Pago de tarjeta") && t.Status == TransactionStatus.APROBADA),
            TotalTransactionsToday = todayTransactions.Count(t => t.Status == TransactionStatus.APROBADA)
        };
    }
}
