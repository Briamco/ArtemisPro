using System;
using System.Collections.Generic;

namespace Application.DTOs.Banking;

public class CreditCardDetailDto
{
    public Guid Id { get; set; }
    public string MaskedCardNumber { get; set; } = string.Empty;
    public string LastFourDigits { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string ClientFullName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal AvailableCredit { get; set; }
    public decimal CurrentDebt { get; set; }
    public string ExpirationDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public IEnumerable<CreditCardTransactionDto> Consumptions { get; set; } = Array.Empty<CreditCardTransactionDto>();
}
