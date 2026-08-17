using System;

namespace Application.DTOs.Banking;

public class LoanAmortizationRowDto
{
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal InstallmentAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal CapitalAmount { get; set; }
    public decimal PendingInstallmentAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public bool IsLate { get; set; }
}
