using System;
using System.Collections.Generic;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class CreditCard : BaseEntity
{
    public Guid ClientId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public decimal Debt { get; set; }
    public string ExpirationDate { get; set; } = string.Empty;
    public string CvcHash { get; set; } = string.Empty;
    public CardStatus Status { get; set; }
    public Guid AdminId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ApplicationUser Client { get; set; } = null!;
    public ApplicationUser Admin { get; set; } = null!;
    public ICollection<CreditCardTransaction> Transactions { get; set; } = new List<CreditCardTransaction>();
}
