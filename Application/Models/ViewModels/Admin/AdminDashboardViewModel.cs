namespace Application.Models.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int TotalHistoricalTransactions { get; set; }
    public int DailyTransactions { get; set; }
    public int TotalHistoricalPayments { get; set; }
    public int DailyPayments { get; set; }
    
    public int ActiveClients { get; set; }
    public int InactiveClients { get; set; }
    public decimal AverageDebtPerClient { get; set; }
    
    public int TotalFinancialProducts { get; set; }
    public int ActiveLoans { get; set; }
    public int ActiveCreditCards { get; set; }
    public int ActiveSavingsAccounts { get; set; }
}