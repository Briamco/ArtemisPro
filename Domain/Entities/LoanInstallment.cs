using System;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class LoanInstallment : BaseEntity
{
    public Guid LoanId { get; set; }
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal CapitalAmount { get; set; }
    public decimal PendingBalance { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public bool IsOverdue { get; set; }

    public Loan Loan { get; set; } = null!;
}
