using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Admin;

public class LoanViewModel
{
    public string Id { get; set; } = string.Empty;
    public string LoanNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientCedula { get; set; } = string.Empty;
    public decimal ApprovedCapital { get; set; }
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal InterestRate { get; set; }
    public int TermInMonths { get; set; }
    public string LoanStatus { get; set; } = string.Empty; 
    public string ClientStatus { get; set; } = string.Empty; 
}