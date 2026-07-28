using System;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Beneficiary : BaseEntity
{
    public Guid ClientId { get; set; }
    public string BeneficiaryAccountNumber { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public BeneficiaryStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public ApplicationUser Client { get; set; } = null!;
}
