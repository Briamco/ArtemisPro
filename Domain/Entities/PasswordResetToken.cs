using System;
using Domain.Common;

namespace Domain.Entities;

public class PasswordResetToken : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
}
