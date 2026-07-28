using System;
using System.Threading.Tasks;
using Application.DTOs.Identity;
using Microsoft.AspNetCore.Identity;

namespace Application.Interfaces.Services;

public interface IAuthAppService
{
    Task<IdentityResult> RegisterAsync(CreateUserDto createUserDto, string confirmationLinkFormat, bool isApiUser = false);
    Task<SignInResult> LoginAsync(LoginDto loginDto);
    Task<WebLoginResult> WebLoginAsync(LoginDto dto);
    Task<ApiLoginResult> ApiLoginAsync(LoginDto dto);
    Task LogoutAsync();
    Task<IdentityResult> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto);
    Task<IdentityResult> ActivateAccountAsync(string email, string token);
    Task<bool> ForgotPasswordAsync(string userName, string resetPasswordLinkFormat);
    Task<bool> ResendActivationEmailAsync(string userName, string confirmationLinkFormat);
    Task<IdentityResult> ResetPasswordAsync(string email, string token, string newPassword);
}
