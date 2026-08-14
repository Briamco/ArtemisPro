using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

public interface IWithdrawalAppService
{
    Task<WithdrawalPreviewDto?> GetWithdrawalPreviewAsync(string accountNumber);
    Task<WithdrawalResult> CreateWithdrawalAsync(System.Guid tellerId, CreateWithdrawalDto dto);
}
