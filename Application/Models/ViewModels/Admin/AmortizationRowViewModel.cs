namespace Application.Models.ViewModels.Admin;

public class AmortizationRowViewModel
{
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal InstallmentValue { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal CapitalAmount { get; set; }
    public decimal PendingBalance { get; set; }
    public string PaymentStatus { get; set; } = string.Empty; 
    public bool IsOverdue { get; set; }
}