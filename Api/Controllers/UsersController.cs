using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs.Identity;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class UsersController : ControllerBase
{
    private readonly IUserAppService _userAppService;

    public UsersController(IUserAppService userAppService)
    {
        _userAppService = userAppService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? role = null)
    {
        if (page < 1 || pageSize < 1)
            return BadRequest(new { message = "Los parámetros de paginación deben ser mayores que cero." });

        if (!string.IsNullOrWhiteSpace(role))
        {
            var r = role.ToLowerInvariant();
            if (r != "administrador" && r != "cajero" && r != "cliente")
                return BadRequest(new { message = "El rol filtrado solo puede ser administrador, cajero o cliente." });
        }

        var result = await _userAppService.GetUsersPagedApiAsync(page, pageSize, role);
        return Ok(result);
    }

    [HttpGet("commerce")]
    public async Task<IActionResult> GetCommerceUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1)
            return BadRequest(new { message = "Los parámetros de paginación deben ser mayores que cero." });

        var result = await _userAppService.GetCommerceUsersPagedApiAsync(page, pageSize);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserApiDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, errorCode, errorMessage, user) = await _userAppService.CreateUserApiAsync(dto);
        if (!success)
        {
            if (errorCode == "Conflict")
                return Conflict(new { message = errorMessage });
            return BadRequest(new { message = errorMessage });
        }

        return StatusCode(201, user);
    }

    [HttpPost("commerce/{commerceId}")]
    public async Task<IActionResult> CreateCommerceUser(Guid commerceId, [FromBody] CreateCommerceUserApiDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, errorCode, errorMessage, user) = await _userAppService.CreateCommerceUserApiAsync(commerceId, dto);
        if (!success)
        {
            if (errorCode == "NotFound")
                return NotFound(new { message = errorMessage });
            if (errorCode == "Conflict")
                return Conflict(new { message = errorMessage });
            return BadRequest(new { message = errorMessage });
        }

        return StatusCode(201, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserApiDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, errorCode, errorMessage) = await _userAppService.UpdateUserApiAsync(id, dto);
        if (!success)
        {
            if (errorCode == "NotFound")
                return NotFound(new { message = errorMessage });
            if (errorCode == "Conflict")
                return Conflict(new { message = errorMessage });
            return BadRequest(new { message = errorMessage });
        }

        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusApiDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid adminId = Guid.Empty;
        if (!string.IsNullOrEmpty(adminIdClaim))
            Guid.TryParse(adminIdClaim, out adminId);

        var (success, errorCode, errorMessage) = await _userAppService.UpdateUserStatusApiAsync(id, dto.Status, adminId);
        if (!success)
        {
            if (errorCode == "Forbidden")
                return StatusCode(403, new { message = errorMessage });
            if (errorCode == "NotFound")
                return NotFound(new { message = errorMessage });
            return BadRequest(new { message = errorMessage });
        }

        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserDetail(Guid id)
    {
        var user = await _userAppService.GetUserDetailApiAsync(id);
        if (user == null)
            return NotFound(new { message = "El usuario indicado no existe." });

        return Ok(user);
    }
}
