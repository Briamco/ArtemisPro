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

    Task<PagedResultDto<SavingsAccountApiDto>> GetSavingsAccountsPagedApiAsync(int page, int pageSize, string? status, string? type, string? identification);
    Task<(bool Success, string? ErrorCode, string? ErrorMessage, SavingsAccountApiDto? Account)> CreateSavingsAccountApiAsync(CreateSavingsAccountApiDto dto, Guid? adminId);
    Task<SavingsAccountDetailWithTransactionsApiDto?> GetAccountTransactionsApiAsync(string accountNumber, int page, int pageSize);
    Task<(bool Success, string? ErrorCode, string? ErrorMessage)> CancelSavingsAccountApiAsync(string accountNumber);
}
