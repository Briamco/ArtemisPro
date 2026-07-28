using System;
using System.Collections.Generic;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Loan : BaseEntity
{
    public Guid ClientId { get; set; }
    public string LoanNumber { get; set; } = string.Empty;
    public decimal ApprovedAmount { get; set; }
    public int Term { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public LoanStatus Status { get; set; }
    public Guid AdminId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ApplicationUser Client { get; set; } = null!;
    public ApplicationUser Admin { get; set; } = null!;
    public ICollection<LoanInstallment> Installments { get; set; } = new List<LoanInstallment>();
}
