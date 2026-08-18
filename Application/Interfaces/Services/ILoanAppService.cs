using System;
using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

public interface ILoanAppService
{
    Task<PagedResultDto<LoanDto>> GetLoansAsync(int page, int pageSize, string? status = null, string? identification = null);
    Task<IEnumerable<LoanDto>> GetAllLoansAsync();
    Task<LoanDetailDto?> GetLoanByIdAsync(Guid id);
    Task<IEnumerable<LoanInstallmentDto>> GetInstallmentsAsync(Guid loanId);
    Task<LoanCreationResponseDto?> CreateLoanAsync(CreateLoanDto dto, Guid adminId);
    Task<(bool Success, string? Error)> UpdateLoanRateAsync(Guid id, UpdateLoanRateDto dto);
    Task<(decimal AverageDebt, bool HasClients)> GetAverageDebtAsync();
    Task<IEnumerable<LoanDto>> GetClientLoansAsync(Guid clientId);
}
