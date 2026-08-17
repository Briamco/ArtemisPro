using System;

namespace Application.DTOs.Banking;

public class LoanCreationResponseDto
{
    public Guid Id { get; set; }
    public string LoanNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string ClientFullName { get; set; } = string.Empty;
    public decimal CapitalAmount { get; set; }
    public int TermInMonths { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public decimal MonthlyInstallment { get; set; }
    public decimal TotalAmountToPay { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
