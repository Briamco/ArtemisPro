using System;
using System.Collections.Generic;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Merchant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string RNC { get; set; } = string.Empty;
    public MerchantStatus Status { get; set; } = MerchantStatus.Activo;
    public Guid? AdminId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}
