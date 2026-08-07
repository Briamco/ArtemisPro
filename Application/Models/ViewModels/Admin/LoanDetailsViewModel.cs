namespace Application.Models.ViewModels.Admin;

public class LoanDetailsViewModel
{
    public string LoanNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public decimal ApprovedAmount { get; set; }
    public decimal InterestRate { get; set; }
    public int TermInMonths { get; set; }
    public string LoanStatus { get; set; } = string.Empty;
    public decimal PendingBalance { get; set; }
    public decimal MonthlyQuote { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime NextDueDate { get; set; }
    public double PaymentProgress { get; set; } 
    public List<AmortizationRowViewModel> AmortizationTable { get; set; } = new();
}