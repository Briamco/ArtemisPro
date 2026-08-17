using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

public interface ILoanAppService
{
    Task<IEnumerable<LoanDto>> GetLoansAsync(string? status = null, string? cedula = null);
    Task<LoanDto?> GetLoanByIdAsync(Guid id);
    Task<IEnumerable<LoanInstallmentDto>> GetInstallmentsAsync(Guid loanId);
    Task<LoanCreationResult> CreateLoanAsync(CreateLoanDto dto);
    Task<(bool Success, string? Error)> UpdateLoanRateAsync(Guid id, UpdateLoanRateDto dto);
    Task<(decimal AverageDebt, bool HasClients)> GetAverageDebtAsync();
    Task<IEnumerable<LoanDto>> GetClientLoansAsync(Guid clientId);
}
