using Application.DTOs.Identity;
using Application.Interfaces.Services;
using Application.Models.ViewModels.Account;
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
        {
          if (User.IsInRole("Administrador"))
            return RedirectToAction("Index", "Admin");
            
          if (User.IsInRole("Cajero"))
            return RedirectToAction("Index", "Cashier");
            
          if (User.IsInRole("Cliente"))
            return RedirectToAction("Index", "Client");

          return RedirectToAction("Index", "Home");
       }

      return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var dto = new LoginDto { UserName = model.UserName, Password = model.Password };
        var result = await _authService.WebLoginAsync(dto);
        if (result.Succeeded)
            return RedirectToAction(result.RedirectAction, result.RedirectController);

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Los datos de acceso son inválidos.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
    public async Task<IActionResult> ForgotPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var resetLinkFormat = Url.Action("ResetPassword", "Account", new { email = "{0}", token = "{1}" }, Request.Scheme);
        var result = await _authService.ForgotPasswordAsync(model.UserName, resetLinkFormat!);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Ocurrió un error al procesar la solicitud.");
            return View(model);
        }

        ViewBag.Message = "Se ha enviado un enlace de restablecimiento de contraseña al correo electrónico registrado.";
        return View("ForgotPasswordConfirmation");
    }

    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            return RedirectToAction("Login");

        var model = new NewPasswordViewModel { Email = email, Token = token };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(NewPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.ResetPasswordAsync(model.Email, model.Token, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        TempData["SuccessMessage"] = "Su contraseña ha sido restablecida correctamente. Ya puede iniciar sesión.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
      return View();
    }

    [HttpGet]
    public async Task<IActionResult> Activate(string email, string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            TempData["ErrorMessage"] = "El enlace de activación no es válido.";
            return RedirectToAction("Login");
        }

        var result = await _authService.ActivateAccountAsync(email, token);
        if (!result.Succeeded)
        {
            var errorMessage = result.Errors.FirstOrDefault()?.Description ?? "El enlace de activación no es válido.";
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction("Login");
        }

        TempData["SuccessMessage"] = "Su cuenta ha sido activada correctamente. Ya puede iniciar sesión.";
        return RedirectToAction("Login");
    }
}
