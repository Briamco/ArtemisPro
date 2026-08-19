using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Identity;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("account")]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAuthAppService _authService;

    public AccountController(IAuthAppService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.ApiLoginAsync(dto);
        if (result.Succeeded)
            return Ok(new { jwt = result.Token, token = result.Token, expires = result.Expires });

        if (result.ErrorMessage != null && result.ErrorMessage.Contains("permisos", System.StringComparison.OrdinalIgnoreCase))
            return StatusCode(403, new { message = result.ErrorMessage });

        return Unauthorized(new { message = result.ErrorMessage ?? "Los datos de acceso son inválidos." });
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmAccountApiDto dto)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(dto.Token))
            return BadRequest(new { message = "El token es obligatorio y debe ser válido." });

        var result = await _authService.ConfirmAccountByTokenAsync(dto.Token);
        if (result.Succeeded)
            return NoContent();

        var errorDesc = result.Errors.FirstOrDefault()?.Description ?? "Token inválido o expirado.";
        return BadRequest(new { message = errorDesc });
    }

    [HttpPost("get-reset-token")]
    public async Task<IActionResult> GetResetToken([FromBody] GetResetTokenApiDto dto)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(dto.UserName))
            return BadRequest(new { message = "El nombre de usuario es obligatorio." });

        var (succeeded, errorMessage) = await _authService.GetResetTokenApiAsync(dto.UserName);
        if (succeeded)
            return NoContent();

        return BadRequest(new { message = errorMessage ?? "No fue posible procesar la solicitud." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordApiDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (dto.Password != dto.ConfirmPassword)
            return BadRequest(new { message = "La contraseña y la confirmación de contraseña deben coincidir." });

        var result = await _authService.ResetPasswordApiAsync(dto.UserId, dto.Token, dto.Password, dto.ConfirmPassword);
        if (result.Succeeded)
            return NoContent();

        var errorDesc = result.Errors.FirstOrDefault()?.Description ?? "Solicitud inválida.";
        return BadRequest(new { message = errorDesc });
    }
}
