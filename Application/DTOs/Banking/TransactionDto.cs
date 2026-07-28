using System;

namespace Application.DTOs.Banking;

public class TransactionDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Beneficiary { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
