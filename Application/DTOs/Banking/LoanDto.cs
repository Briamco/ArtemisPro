using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class LoanDto
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string LoanNumber { get; set; } = string.Empty;
    public decimal ApprovedAmount { get; set; }
    public int Term { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public decimal PendingAmount { get; set; }
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ClientStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
