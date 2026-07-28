using System;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class CreditCardTransaction : BaseEntity
{
    public Guid CreditCardId { get; set; }
    public decimal Amount { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public CreditCardTransactionStatus Status { get; set; }
    public DateTime Date { get; set; }

    public CreditCard CreditCard { get; set; } = null!;
}
