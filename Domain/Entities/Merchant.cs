using System;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Merchant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string RNC { get; set; } = string.Empty;
    public MerchantStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
