using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class CreditCardDto
{
    public Guid Id { get; set; }
    public string MaskedCardNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public decimal Debt { get; set; }
    public string ExpirationDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
