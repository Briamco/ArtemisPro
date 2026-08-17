using System;
using System.Collections.Generic;

namespace Application.DTOs.Banking;

public class LoanDetailDto
{
    public Guid Id { get; set; }
    public string LoanNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string ClientFullName { get; set; } = string.Empty;
    public decimal CapitalAmount { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public int TermInMonths { get; set; }
    public decimal MonthlyInstallment { get; set; }
    public decimal PendingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ClientPaymentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<LoanAmortizationRowDto> Amortization { get; set; } = new();
}
