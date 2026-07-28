using System;
using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class SavingsAccount : BaseEntity
{
    public Guid ClientId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public AccountType AccountType { get; set; }
    public AccountStatus Status { get; set; }
    public Guid? AdminId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ApplicationUser Client { get; set; } = null!;
    public ApplicationUser? Admin { get; set; }
}
