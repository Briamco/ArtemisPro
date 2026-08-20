using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Identity;
using Application.Interfaces.Services;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Moq;
using Shared.Interfaces;
using Xunit;

namespace Application.Tests;

public class AuthAppServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthAppService _authAppService;

    public AuthAppServiceTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            contextAccessorMock.Object,
            claimsFactoryMock.Object,
            null!, null!, null!, null!);

        _emailServiceMock = new Mock<IEmailService>();
        _mapperMock = new Mock<IMapper>();
        _configurationMock = new Mock<IConfiguration>();

        _configurationMock.Setup(c => c["JwtSettings:Key"]).Returns("SuperSecretKeyForTestingJwtTokens1234567890!");
        _configurationMock.Setup(c => c["JwtSettings:Issuer"]).Returns("ArtemisPro");
        _configurationMock.Setup(c => c["JwtSettings:Audience"]).Returns("ArtemisProUsers");

        _authAppService = new AuthAppService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _emailServiceMock.Object,
            _mapperMock.Object,
            _configurationMock.Object);
    }

    [Fact]
    public async Task WebLoginAsync_UserNotFound_ReturnsInvalidCredentialsMessage()
    {
        _userManagerMock.Setup(m => m.FindByNameAsync("nonexistent"))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _authAppService.WebLoginAsync(new LoginDto { UserName = "nonexistent", Password = "Password123!" });

        Assert.False(result.Succeeded);
        Assert.Equal("Los datos de acceso son inválidos.", result.ErrorMessage);
    }

    [Fact]
    public async Task WebLoginAsync_UserInactive_ReturnsInactiveAccountMessage()
    {
        var user = new ApplicationUser { UserName = "john", IsActive = false };
        _userManagerMock.Setup(m => m.FindByNameAsync("john")).ReturnsAsync(user);

        var result = await _authAppService.WebLoginAsync(new LoginDto { UserName = "john", Password = "Password123!" });

        Assert.False(result.Succeeded);
        Assert.Contains("Su cuenta se encuentra inactiva", result.ErrorMessage);
    }

    [Fact]
    public async Task WebLoginAsync_CommerceUser_ReturnsNoWebAccessMessage()
    {
        var user = new ApplicationUser { UserName = "merchant", IsActive = true };
        _userManagerMock.Setup(m => m.FindByNameAsync("merchant")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Comercio" });

        var result = await _authAppService.WebLoginAsync(new LoginDto { UserName = "merchant", Password = "Password123!" });

        Assert.False(result.Succeeded);
        Assert.Equal("Este usuario no tiene permisos para acceder a la aplicación web.", result.ErrorMessage);
    }

    [Fact]
    public async Task WebLoginAsync_ValidAdmin_ReturnsRedirectToAdminHome()
    {
        var user = new ApplicationUser { UserName = "admin", IsActive = true };
        _userManagerMock.Setup(m => m.FindByNameAsync("admin")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Administrador" });
        _signInManagerMock.Setup(s => s.PasswordSignInAsync("admin", "Admin123!", false, false))
            .ReturnsAsync(SignInResult.Success);

        var result = await _authAppService.WebLoginAsync(new LoginDto { UserName = "admin", Password = "Admin123!" });

        Assert.True(result.Succeeded);
        Assert.Equal("Admin", result.RedirectController);
        Assert.Equal("Index", result.RedirectAction);
    }

    [Fact]
    public async Task WebLoginAsync_ValidCashier_ReturnsRedirectToCashierHome()
    {
        var user = new ApplicationUser { UserName = "cajero1", IsActive = true };
        _userManagerMock.Setup(m => m.FindByNameAsync("cajero1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Cajero" });
        _signInManagerMock.Setup(s => s.PasswordSignInAsync("cajero1", "Pass123!", false, false))
            .ReturnsAsync(SignInResult.Success);

        var result = await _authAppService.WebLoginAsync(new LoginDto { UserName = "cajero1", Password = "Pass123!" });

        Assert.True(result.Succeeded);
        Assert.Equal("Cashier", result.RedirectController);
        Assert.Equal("Index", result.RedirectAction);
    }

    [Fact]
    public async Task WebLoginAsync_ValidClient_ReturnsRedirectToClientHome()
    {
        var user = new ApplicationUser { UserName = "cliente1", IsActive = true };
        _userManagerMock.Setup(m => m.FindByNameAsync("cliente1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Cliente" });
        _signInManagerMock.Setup(s => s.PasswordSignInAsync("cliente1", "Pass123!", false, false))
            .ReturnsAsync(SignInResult.Success);

        var result = await _authAppService.WebLoginAsync(new LoginDto { UserName = "cliente1", Password = "Pass123!" });

        Assert.True(result.Succeeded);
        Assert.Equal("Client", result.RedirectController);
        Assert.Equal("Index", result.RedirectAction);
    }

    [Fact]
    public async Task ApiLoginAsync_InactiveUser_ReturnsInactiveErrorMessage()
    {
        var user = new ApplicationUser { UserName = "apiAdmin", IsActive = false };
        _userManagerMock.Setup(m => m.FindByNameAsync("apiAdmin")).ReturnsAsync(user);

        var result = await _authAppService.ApiLoginAsync(new LoginDto { UserName = "apiAdmin", Password = "Password123!" });

        Assert.False(result.Succeeded);
        Assert.Contains("Su cuenta se encuentra inactiva", result.ErrorMessage);
    }

    [Fact]
    public async Task ApiLoginAsync_ValidAdmin_ReturnsJwtToken()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "apiAdmin", Email = "admin@bank.com", IsActive = true };
        _userManagerMock.Setup(m => m.FindByNameAsync("apiAdmin")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Administrador" });
        _signInManagerMock.Setup(s => s.CheckPasswordSignInAsync(user, "Password123!", false))
            .ReturnsAsync(SignInResult.Success);

        var result = await _authAppService.ApiLoginAsync(new LoginDto { UserName = "apiAdmin", Password = "Password123!" });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task ActivateAccountAsync_UserNotFound_ReturnsInvalidTokenError()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync("notfound@test.com")).ReturnsAsync((ApplicationUser?)null);

        var result = await _authAppService.ActivateAccountAsync("notfound@test.com", "anytoken");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Description == "El enlace de activación no es válido.");
    }

    [Fact]
    public async Task ActivateAccountAsync_AlreadyActive_ReturnsTokenAlreadyUsedError()
    {
        var user = new ApplicationUser { Email = "active@test.com", EmailConfirmed = true, IsActive = true };
        _userManagerMock.Setup(m => m.FindByEmailAsync("active@test.com")).ReturnsAsync(user);

        var result = await _authAppService.ActivateAccountAsync("active@test.com", "anytoken");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Description == "Este enlace de activación ya fue utilizado.");
    }

    [Fact]
    public async Task ForgotPasswordAsync_UserNotFound_ReturnsErrorMessage()
    {
        _userManagerMock.Setup(m => m.FindByNameAsync("unknown")).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.FindByEmailAsync("unknown")).ReturnsAsync((ApplicationUser?)null);

        var result = await _authAppService.ForgotPasswordAsync("unknown", "http://localhost/{0}/{1}");

        Assert.False(result.Succeeded);
        Assert.Equal("No existe un usuario registrado con este nombre de usuario.", result.ErrorMessage);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ValidUser_DeactivatesAndSendsEmail()
    {
        var user = new ApplicationUser { UserName = "client1", Email = "client1@test.com", FirstName = "Juan", IsActive = true };
        _userManagerMock.Setup(m => m.FindByNameAsync("client1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Cliente" });
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("raw_reset_token");

        var result = await _authAppService.ForgotPasswordAsync("client1", "http://localhost/Account/ResetPassword?email={0}&token={1}");

        Assert.True(result.Succeeded);
        Assert.False(user.IsActive);
        _emailServiceMock.Verify(e => e.SendAsync("client1@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_ResetsAndActivatesUser()
    {
        var user = new ApplicationUser { Email = "user@test.com", IsActive = false };
        _userManagerMock.Setup(m => m.FindByEmailAsync("user@test.com")).ReturnsAsync(user);

        var rawToken = "valid_token_123";
        var safeToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

        _userManagerMock.Setup(m => m.ResetPasswordAsync(user, rawToken, "NewPassword123!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _authAppService.ResetPasswordAsync("user@test.com", safeToken, "NewPassword123!");

        Assert.True(result.Succeeded);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task GetResetTokenApiAsync_ValidAdmin_DeactivatesAndReturnsToken()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, UserName = "apiAdmin", Email = "admin@api.com", FirstName = "Admin", IsActive = true };
        _userManagerMock.Setup(m => m.FindByNameAsync("apiAdmin")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Administrador" });
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("raw-token");

        var (success, error) = await _authAppService.GetResetTokenApiAsync("apiAdmin");

        Assert.True(success);
        Assert.Null(error);
        Assert.False(user.IsActive);
        _emailServiceMock.Verify(e => e.SendAsync("admin@api.com", "Token de restablecimiento de contraseña", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordApiAsync_PasswordMismatch_ReturnsPasswordMismatchError()
    {
        var result = await _authAppService.ResetPasswordApiAsync(Guid.NewGuid().ToString(), "token", "Pass1", "Pass2");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "PasswordMismatch");
    }
}
