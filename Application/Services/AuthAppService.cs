using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Identity;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shared.Interfaces;

namespace Application.Services;

public class AuthAppService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IEmailService emailService,
    IMapper mapper,
    IConfiguration configuration) : IAuthAppService
{
    public async Task<IdentityResult> RegisterAsync(CreateUserDto createUserDto, string confirmationLinkFormat, bool isApiUser = false)
    {
        var user = mapper.Map<ApplicationUser>(createUserDto);

        if (isApiUser)
        {
            user.IsActive = true;
            user.EmailConfirmed = true;
        }

        var result = await userManager.CreateAsync(user, createUserDto.Password);
        if (!result.Succeeded)
            return result;

        if (!string.IsNullOrWhiteSpace(createUserDto.Role))
        {
            await userManager.AddToRoleAsync(user, createUserDto.Role);
        }

        if (isApiUser)
            return result;

        var rawToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var safeToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        var confirmationLink = string.Format(confirmationLinkFormat, Uri.EscapeDataString(user.Email!), safeToken);

        var subject = "Activa tu cuenta - Artemis Banking Pro";
        var body = $"""
            <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e1e4e6; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05);">
                <div style="background-color: #1a237e; padding: 20px; text-align: center;">
                    <h1 style="color: white; margin: 0; font-size: 24px; font-weight: bold; letter-spacing: 0.5px;">Artemis Banking Pro</h1>
                </div>
                <div style="padding: 30px; color: #333333; line-height: 1.6;">
                    <h2 style="color: #1a237e; margin-top: 0; font-size: 20px;">¡Bienvenido a Artemis Banking Pro, {user.FirstName}!</h2>
                    <p>Tu cuenta ha sido creada con éxito. Para activarla y poder iniciar sesión, por favor haz clic en el siguiente botón:</p>
                    <p style="margin: 30px 0; text-align: center;">
                        <a href="{confirmationLink}" style="background-color: #1a237e; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;">Activar Cuenta</a>
                    </p>
                    <p style="font-size: 14px; color: #666666;">Si el botón no funciona, puedes copiar y pegar el siguiente enlace en tu navegador:</p>
                    <p style="font-size: 14px; word-break: break-all;"><a href="{confirmationLink}" style="color: #1a237e; text-decoration: none;">{confirmationLink}</a></p>
                </div>
                <div style="background-color: #f8f9fa; padding: 15px 30px; text-align: center; font-size: 12px; color: #666666; border-top: 1px solid #e1e4e6;">
                    Este es un correo automático, por favor no respondas a este mensaje.<br/>
                    &copy; {DateTime.UtcNow.Year} Artemis Banking Pro. Todos los derechos reservados.
                </div>
            </div>
            """;

        await emailService.SendAsync(user.Email!, subject, body);

        return result;
    }

    public async Task<SignInResult> LoginAsync(LoginDto loginDto)
    {
        var user = await userManager.FindByNameAsync(loginDto.UserName);
        if (user == null)
            return SignInResult.Failed;

        if (!user.IsActive)
            return SignInResult.NotAllowed;

        return await signInManager.PasswordSignInAsync(user.UserName!, loginDto.Password, loginDto.RememberMe, lockoutOnFailure: true);
    }

    public async Task<WebLoginResult> WebLoginAsync(LoginDto dto)
    {
        var user = await userManager.FindByNameAsync(dto.UserName);
        if (user == null)
            return new WebLoginResult { ErrorMessage = "Los datos de acceso son inválidos." };

        if (!user.IsActive)
            return new WebLoginResult { ErrorMessage = "Su cuenta se encuentra inactiva. Debe activar su cuenta mediante el enlace enviado a su correo electrónico registrado para poder acceder al sistema." };

        var roles = await userManager.GetRolesAsync(user);
        var webRoles = new[] { "Administrador", "Cajero", "Cliente" };
        if (!roles.Any(r => webRoles.Contains(r)))
            return new WebLoginResult { ErrorMessage = "Este usuario no tiene permisos para acceder a la aplicación web." };

        var result = await signInManager.PasswordSignInAsync(user.UserName!, dto.Password, dto.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
            return new WebLoginResult { ErrorMessage = "Los datos de acceso son inválidos." };

        var primaryRole = roles.First(r => webRoles.Contains(r));
        var (controller, action) = primaryRole switch
        {
            "Administrador" => ("Admin", "Index"),
            "Cajero" => ("Cajero", "Index"),
            "Cliente" => ("Cliente", "Index"),
            _ => ("Home", "Index")
        };

        return new WebLoginResult
        {
            Succeeded = true,
            RedirectController = controller,
            RedirectAction = action
        };
    }

    public async Task<ApiLoginResult> ApiLoginAsync(LoginDto dto)
    {
        var user = await userManager.FindByNameAsync(dto.UserName);
        if (user == null)
            return new ApiLoginResult { ErrorMessage = "Los datos de acceso son inválidos." };

        if (!user.IsActive)
            return new ApiLoginResult { ErrorMessage = "Su cuenta se encuentra inactiva." };

        var roles = await userManager.GetRolesAsync(user);
        var allowedRoles = new[] { "Administrador", "Cajero", "Comercio" };
        if (!roles.Any(r => allowedRoles.Contains(r)))
            return new ApiLoginResult { ErrorMessage = "Este usuario no tiene permisos para acceder a la API." };

        var result = await signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded)
            return new ApiLoginResult { ErrorMessage = "Los datos de acceso son inválidos." };

        var token = GenerateJwtToken(user, roles);
        return new ApiLoginResult
        {
            Succeeded = true,
            Token = token,
            Expires = DateTime.UtcNow.AddHours(2)
        };
    }

    public async Task LogoutAsync() =>
        await signInManager.SignOutAsync();

    public async Task<IdentityResult> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "Usuario no encontrado." });

        return await userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
    }

    public async Task<IdentityResult> ActivateAccountAsync(string email, string token)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "El enlace de activación no es válido." });

        if (user.EmailConfirmed && user.IsActive)
        {
            return IdentityResult.Failed(new IdentityError { Code = "TokenAlreadyUsed", Description = "Este enlace de activación ya fue utilizado." });
        }

        try
        {
            var decodedTokenBytes = WebEncoders.Base64UrlDecode(token);
            var rawToken = Encoding.UTF8.GetString(decodedTokenBytes);

            var result = await userManager.ConfirmEmailAsync(user, rawToken);
            if (result.Succeeded)
            {
                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    return updateResult;
            }
            else
            {
                if (user.EmailConfirmed)
                {
                    return IdentityResult.Failed(new IdentityError { Code = "TokenAlreadyUsed", Description = "Este enlace de activación ya fue utilizado." });
                }
                return IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "El enlace de activación no es válido." });
            }

            return result;
        }
        catch
        {
            return IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "El enlace de activación no es válido." });
        }
    }

    public async Task<ForgotPasswordResult> ForgotPasswordAsync(string userName, string resetPasswordLinkFormat)
    {
        var user = await userManager.FindByNameAsync(userName);
        user ??= await userManager.FindByEmailAsync(userName);
        if (user == null)
            return new ForgotPasswordResult { ErrorMessage = "No existe un usuario registrado con este nombre de usuario." };

        if (string.IsNullOrWhiteSpace(user.Email))
            return new ForgotPasswordResult { ErrorMessage = "Este usuario no tiene un correo electrónico registrado. No es posible enviar la solicitud de restablecimiento." };

        var roles = await userManager.GetRolesAsync(user);
        var webRoles = new[] { "Administrador", "Cajero", "Cliente" };
        if (!roles.Any(r => webRoles.Contains(r)))
            return new ForgotPasswordResult { ErrorMessage = "Este usuario no tiene permisos para acceder a la aplicación web." };

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var rawToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var safeToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        var resetLink = string.Format(resetPasswordLinkFormat, Uri.EscapeDataString(user.Email!), safeToken);

        var subject = "Restablece tu contraseña - Artemis Banking Pro";
        var body = $"""
            <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e1e4e6; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05);">
                <div style="background-color: #1a237e; padding: 20px; text-align: center;">
                    <h1 style="color: white; margin: 0; font-size: 24px; font-weight: bold; letter-spacing: 0.5px;">Artemis Banking Pro</h1>
                </div>
                <div style="padding: 30px; color: #333333; line-height: 1.6;">
                    <h2 style="color: #1a237e; margin-top: 0; font-size: 20px;">¡Hola, {user.FirstName}!</h2>
                    <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta en Artemis Banking Pro. Si realizaste esta solicitud, por favor haz clic en el siguiente botón:</p>
                    <p style="margin: 30px 0; text-align: center;">
                        <a href="{resetLink}" style="background-color: #1a237e; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;">Restablecer Contraseña</a>
                    </p>
                    <p style="font-size: 14px; color: #666666;">Si el botón no funciona, puedes copiar y pegar el siguiente enlace en tu navegador:</p>
                    <p style="font-size: 14px; word-break: break-all;"><a href="{resetLink}" style="color: #1a237e; text-decoration: none;">{resetLink}</a></p>
                    <br/>
                    <p style="font-size: 14px; color: #666666; margin-top: 20px;">Si no solicitaste este cambio, puedes ignorar este correo de forma segura.</p>
                </div>
                <div style="background-color: #f8f9fa; padding: 15px 30px; text-align: center; font-size: 12px; color: #666666; border-top: 1px solid #e1e4e6;">
                    Este es un correo automático, por favor no respondas a este mensaje.<br/>
                    &copy; {DateTime.UtcNow.Year} Artemis Banking Pro. Todos los derechos reservados.
                </div>
            </div>
            """;

        await emailService.SendAsync(user.Email!, subject, body);
        return new ForgotPasswordResult { Succeeded = true };
    }

    public async Task<bool> ResendActivationEmailAsync(string userName, string confirmationLinkFormat)
    {
        var user = await userManager.FindByEmailAsync(userName);
        user ??= await userManager.FindByNameAsync(userName);
        if (user == null || user.IsActive)
            return false;

        var rawToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var safeToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        var confirmationLink = string.Format(confirmationLinkFormat, Uri.EscapeDataString(user.Email!), safeToken);

        var subject = "Activa tu cuenta - Artemis Banking Pro";
        var body = $"""
            <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e1e4e6; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05);">
                <div style="background-color: #1a237e; padding: 20px; text-align: center;">
                    <h1 style="color: white; margin: 0; font-size: 24px; font-weight: bold; letter-spacing: 0.5px;">Artemis Banking Pro</h1>
                </div>
                <div style="padding: 30px; color: #333333; line-height: 1.6;">
                    <h2 style="color: #1a237e; margin-top: 0; font-size: 20px;">¡Hola de nuevo, {user.FirstName}!</h2>
                    <p>Aquí tienes tu nuevo enlace para activar tu cuenta en Artemis Banking Pro. Por favor haz clic en el siguiente botón:</p>
                    <p style="margin: 30px 0; text-align: center;">
                        <a href="{confirmationLink}" style="background-color: #1a237e; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;">Activar Cuenta</a>
                    </p>
                    <p style="font-size: 14px; color: #666666;">Si el botón no funciona, puedes copiar y pegar el siguiente enlace en tu navegador:</p>
                    <p style="font-size: 14px; word-break: break-all;"><a href="{confirmationLink}" style="color: #1a237e; text-decoration: none;">{confirmationLink}</a></p>
                </div>
                <div style="background-color: #f8f9fa; padding: 15px 30px; text-align: center; font-size: 12px; color: #666666; border-top: 1px solid #e1e4e6;">
                    Este es un correo automático, por favor no respondas a este mensaje.<br/>
                    &copy; {DateTime.UtcNow.Year} Artemis Banking Pro. Todos los derechos reservados.
                </div>
            </div>
            """;

        await emailService.SendAsync(user.Email!, subject, body);

        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return true;
    }

    public async Task<IdentityResult> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "El enlace de restablecimiento no es válido." });

        try
        {
            var decodedTokenBytes = WebEncoders.Base64UrlDecode(token);
            var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);

            var result = await userManager.ResetPasswordAsync(user, decodedToken, newPassword);
            if (result.Succeeded)
            {
                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
                await userManager.UpdateAsync(user);
                await userManager.ResetAccessFailedCountAsync(user);
                await userManager.SetLockoutEndDateAsync(user, null);
                return result;
            }

            var errors = result.Errors.Select(err =>
            {
                if (err.Code == "InvalidToken")
                {
                    return new IdentityError
                    {
                        Code = "InvalidToken",
                        Description = "El enlace de restablecimiento ha expirado. Solicite un nuevo restablecimiento de contraseña."
                    };
                }
                return err;
            }).ToList();

            return IdentityResult.Failed(errors.ToArray());
        }
        catch
        {
            return IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "El enlace de restablecimiento no es válido." });
        }
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: configuration["JwtSettings:Issuer"],
            audience: configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
