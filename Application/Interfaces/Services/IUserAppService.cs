using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Identity;

namespace Application.Interfaces.Services;

public interface IUserAppService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync(string? role = null);
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<(bool Success, string? Error)> CreateUserAsync(CreateUserDto dto, string confirmationLinkFormat);
    Task<(bool Success, string? Error)> EditUserAsync(Guid id, EditUserDto dto);
    Task<(bool Success, string? Error)> ToggleUserStatusAsync(Guid id);
}
