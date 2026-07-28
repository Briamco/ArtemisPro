using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class LoanInstallmentDto
{
    public Guid Id { get; set; }
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal CapitalAmount { get; set; }
    public decimal PendingBalance { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
}
