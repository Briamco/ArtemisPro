using Application.DTOs.Identity;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
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
            return Ok(new { token = result.Token, expires = result.Expires });

        return Unauthorized(new { message = result.ErrorMessage ?? "Los datos de acceso son inválidos." });
    }
}
