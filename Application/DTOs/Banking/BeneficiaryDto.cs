using System;

namespace Application.DTOs.Banking;

public class BeneficiaryDto
{
    public Guid Id { get; set; }
    public string BeneficiaryAccountNumber { get; set; } = string.Empty;
    public string OwnerFirstName { get; set; } = string.Empty;
    public string OwnerLastName { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
