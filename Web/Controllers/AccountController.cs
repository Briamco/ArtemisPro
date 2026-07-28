using Application.DTOs.Identity;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthAppService _authService;

    public AccountController(IAuthAppService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var result = await _authService.WebLoginAsync(dto);
        if (result.Succeeded)
            return RedirectToAction(result.RedirectAction, result.RedirectController);

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Los datos de acceso son inválidos.");
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var resetLinkFormat = $"{Request.Scheme}://{Request.Host}/Account/ResetPassword?email={{0}}&token={{1}}";
        await _authService.ForgotPasswordAsync(dto.UserName, resetLinkFormat);

        ViewBag.Message = "Se ha enviado un enlace de restablecimiento de contraseña al correo electrónico registrado.";
        return View("ForgotPasswordConfirmation");
    }

    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            return RedirectToAction("Login");

        ViewBag.Email = email;
        ViewBag.Token = token;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string email, string token, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            return RedirectToAction("Login");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            ModelState.AddModelError(string.Empty, "La contraseña debe tener al menos 8 caracteres.");
            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, "La contraseña y la confirmación no coinciden.");
            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        var result = await _authService.ResetPasswordAsync(email, token, newPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        ViewBag.Message = "Su contraseña ha sido restablecida correctamente. Ya puede iniciar sesión.";
        return View("ResetPasswordConfirmation");
    }

    [HttpGet]
    public async Task<IActionResult> Activate(string email, string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            return RedirectToAction("Login");

        var result = await _authService.ActivateAccountAsync(email, token);
        if (!result.Succeeded)
        {
            var errorMessage = result.Errors.FirstOrDefault()?.Description ?? "El enlace de activación es inválido o ha expirado.";
            ViewBag.ErrorMessage = errorMessage;
            return View("ActivationError");
        }

        ViewBag.Message = "Su cuenta ha sido activada correctamente. Ya puede iniciar sesión.";
        return View("ActivationSuccess");
    }
}
