using System;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid SavingsAccountId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Beneficiary { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public DateTime Date { get; set; }

    public SavingsAccount SavingsAccount { get; set; } = null!;
}
