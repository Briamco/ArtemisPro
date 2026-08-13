using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

public interface IPaymentAppService
{
    Task<(bool Success, string? Error)> PayCreditCardAsync(PayCreditCardDto dto);
    Task<(bool Success, string? Error)> PayLoanAsync(PayLoanDto dto);
}
