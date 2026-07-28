using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

public interface IBeneficiaryAppService
{
    Task<IEnumerable<BeneficiaryDto>> GetByClientIdAsync(Guid clientId);
    Task<(bool Success, string? Error)> CreateBeneficiaryAsync(Guid clientId, CreateBeneficiaryDto dto);
    Task<(bool Success, string? Error)> DeleteBeneficiaryAsync(Guid id);
}
