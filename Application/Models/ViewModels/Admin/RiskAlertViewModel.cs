namespace Application.Models.ViewModels.Admin;

public class RiskAlertViewModel
{
    public string ClientId { get; set; } = string.Empty;
    public decimal CurrentDebt { get; set; }
    public decimal ProjectedDebt { get; set; }
    public decimal SystemAverage { get; set; }
    public string WarningMessage { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal InterestRate { get; set; }
    public int TermInMonths { get; set; }
}