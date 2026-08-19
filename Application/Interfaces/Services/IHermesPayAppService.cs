using System;
using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

public interface IHermesPayAppService
{
    Task<(bool Success, string? ErrorCode, string? ErrorMessage, CommerceTransactionsResponseDto? Result)> GetCommerceTransactionsAsync(Guid commerceId, int page, int pageSize);
    Task<(bool Success, string? ErrorCode, string? ErrorMessage)> ProcessPaymentAsync(Guid commerceId, ProcessPaymentDto dto);
}
