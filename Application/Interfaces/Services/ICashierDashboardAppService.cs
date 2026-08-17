using System;
using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

public interface ICashierDashboardAppService
{
    Task<CashierDashboardStatsDto> GetTellerDailyStatsAsync(Guid tellerId);
}
