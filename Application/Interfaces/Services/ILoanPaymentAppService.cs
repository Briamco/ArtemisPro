using System;
using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

public interface ILoanPaymentAppService
{
    Task<LoanPaymentPreviewResult> GetLoanPaymentPreviewAsync(string accountNumber, string loanNumber, decimal amount);
    Task<LoanPaymentResult> CreateLoanPaymentAsync(Guid tellerId, CreateLoanPaymentDto dto);
}
