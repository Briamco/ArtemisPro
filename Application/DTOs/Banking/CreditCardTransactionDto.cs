using System;

namespace Application.DTOs.Banking;

public class CreditCardTransactionDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string CommerceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    // Backward-compatible alias
    public string MerchantName { get => CommerceName; set => CommerceName = value; }
}
