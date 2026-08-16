using System;
using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

public interface IThirdPartyTransactionAppService
{
    Task<ThirdPartyTransactionPreviewResult> GetPreviewAsync(string sourceAccountNumber, string destinationAccountNumber, decimal amount);
    Task<ThirdPartyTransactionResult> CreateTransactionAsync(Guid tellerId, CreateThirdPartyTransactionDto dto);
}
