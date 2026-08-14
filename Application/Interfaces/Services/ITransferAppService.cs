using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

public interface ITransferAppService
{
    Task<IEnumerable<SavingsAccountDto>> GetActiveSavingsAccountsByClientIdAsync(Guid clientId);
    Task<TransferResult> CreateTransferAsync(Guid clientId, CreateTransferDto dto);
}
