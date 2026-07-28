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
    Task<(bool Success, string? Error, string? WarningMessage, bool IsHighRisk)> CreateLoanAsync(CreateLoanDto dto);
    Task<(bool Success, string? Error)> UpdateLoanRateAsync(Guid id, UpdateLoanRateDto dto);
    Task<(decimal AverageDebt, bool HasClients)> GetAverageDebtAsync();
}
