using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

public interface IDepositAppService
{
    Task<DepositPreviewDto?> GetDepositPreviewAsync(string accountNumber);
    Task<DepositResult> CreateDepositAsync(System.Guid tellerId, CreateDepositDto dto);
}
