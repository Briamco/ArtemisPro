namespace Application.Models.ViewModels.Cashier;

public class CashierHomeViewModel
{
    public int TotalTransactionsToday { get; set; }
    public int TotalPaymentsToday { get; set; }
    public int TotalDepositsToday { get; set; }
    public int TotalWithdrawalsToday { get; set; }
}