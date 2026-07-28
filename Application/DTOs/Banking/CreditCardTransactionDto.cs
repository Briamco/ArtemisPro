using System;

namespace Application.DTOs.Banking;

public class CreditCardTransactionDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
