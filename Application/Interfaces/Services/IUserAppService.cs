using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.DTOs.Identity;

namespace Application.Interfaces.Services;

public interface IUserAppService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync(string? role = null);
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<(bool Success, string? Error)> CreateUserAsync(CreateUserDto dto, string confirmationLinkFormat);
    Task<(bool Success, string? Error)> EditUserAsync(Guid id, EditUserDto dto);
    Task<(bool Success, string? Error)> ToggleUserStatusAsync(Guid id);

    Task<PagedResultDto<UserApiDto>> GetUsersPagedApiAsync(int page, int pageSize, string? role);
    Task<PagedResultDto<CommerceUserApiDto>> GetCommerceUsersPagedApiAsync(int page, int pageSize);
    Task<(bool Success, string? ErrorCode, string? ErrorMessage, CreatedUserResponseApiDto? User)> CreateUserApiAsync(CreateUserApiDto dto);
    Task<(bool Success, string? ErrorCode, string? ErrorMessage, CreatedUserResponseApiDto? User)> CreateCommerceUserApiAsync(Guid commerceId, CreateCommerceUserApiDto dto);
    Task<(bool Success, string? ErrorCode, string? ErrorMessage)> UpdateUserApiAsync(Guid id, UpdateUserApiDto dto);
    Task<(bool Success, string? ErrorCode, string? ErrorMessage)> UpdateUserStatusApiAsync(Guid id, bool status, Guid adminId);
    Task<UserDetailApiDto?> GetUserDetailApiAsync(Guid id);
}
