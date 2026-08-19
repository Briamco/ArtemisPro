using System;
using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

public interface ICommerceAppService
{
    Task<PagedResultDto<CommerceDto>> GetCommercesPagedAsync(int page, int pageSize, string? status);
    Task<CommerceDetailDto?> GetCommerceByIdAsync(Guid id);
    Task<(bool Success, string? ErrorCode, string? ErrorMessage, CommerceDto? Commerce)> CreateCommerceAsync(CreateCommerceDto dto, Guid? adminId);
    Task<(bool Success, string? ErrorCode, string? ErrorMessage)> UpdateCommerceAsync(Guid id, UpdateCommerceDto dto);
    Task<(bool Success, string? ErrorCode, string? ErrorMessage)> UpdateCommerceStatusAsync(Guid id, bool status);
}
