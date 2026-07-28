using System;

namespace Application.DTOs.Banking;

public class SavingsAccountDto
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
