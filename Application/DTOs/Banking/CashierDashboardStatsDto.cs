namespace Application.DTOs.Banking;

public class CashierDashboardStatsDto
{
    public int TotalDepositsToday { get; set; }
    public int TotalWithdrawalsToday { get; set; }
    public int TotalPaymentsToday { get; set; }
    public int TotalTransactionsToday { get; set; }
}
