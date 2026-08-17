using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

public interface ISavingsAccountAppService
{
    Task<IEnumerable<SavingsAccountDto>> GetSavingsAccountsAsync(string? status = null, string? type = null, string? cedula = null);
    Task<SavingsAccountDto?> GetSavingsAccountByIdAsync(Guid id);
    Task<IEnumerable<TransactionDto>> GetTransactionsAsync(Guid accountId);
    Task<(bool Success, string? Error)> CreateSavingsAccountAsync(CreateSavingsAccountDto dto);
    Task<(bool Success, string? Error)> CancelSavingsAccountAsync(Guid id);
    Task<IEnumerable<SavingsAccountDto>> GetClientAccountsAsync(Guid clientId);
}
