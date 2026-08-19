using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/savings-account")]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class SavingsAccountController : ControllerBase
{
    private readonly ISavingsAccountAppService _savingsAccountAppService;

    public SavingsAccountController(ISavingsAccountAppService savingsAccountAppService)
    {
        _savingsAccountAppService = savingsAccountAppService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSavingsAccounts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] string? identification = null)
    {
        if (page < 1 || pageSize < 1)
            return BadRequest(new { message = "Los parámetros de paginación deben ser mayores que cero." });

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.ToLowerInvariant();
            if (s != "activa" && s != "cancelada" && s != "todas")
                return BadRequest(new { message = "El estado filtrado solo puede ser activa, cancelada o todas." });
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var t = type.ToLowerInvariant();
            if (t != "principal" && t != "secundaria" && t != "todas")
                return BadRequest(new { message = "El tipo filtrado solo puede ser principal, secundaria o todas." });
        }

        var result = await _savingsAccountAppService.GetSavingsAccountsPagedApiAsync(page, pageSize, status, type, identification);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSavingsAccount([FromBody] CreateSavingsAccountApiDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? adminId = null;
        if (!string.IsNullOrEmpty(adminIdClaim) && Guid.TryParse(adminIdClaim, out var parsedAdminId))
            adminId = parsedAdminId;

        var (success, errorCode, errorMessage, account) = await _savingsAccountAppService.CreateSavingsAccountApiAsync(dto, adminId);
        if (!success)
        {
            if (errorCode == "NotFound")
                return NotFound(new { message = errorMessage });
            return BadRequest(new { message = errorMessage });
        }

        return StatusCode(201, account);
    }

    [HttpGet("{accountNumber}/transactions")]
    public async Task<IActionResult> GetAccountTransactions(
        string accountNumber,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1)
            return BadRequest(new { message = "Los parámetros de paginación deben ser mayores que cero." });

        var result = await _savingsAccountAppService.GetAccountTransactionsApiAsync(accountNumber, page, pageSize);
        if (result == null)
            return NotFound(new { message = "La cuenta indicada no existe." });

        return Ok(result);
    }

    [HttpPatch("{accountNumber}/cancel")]
    public async Task<IActionResult> CancelSavingsAccount(string accountNumber)
    {
        var (success, errorCode, errorMessage) = await _savingsAccountAppService.CancelSavingsAccountApiAsync(accountNumber);
        if (!success)
        {
            if (errorCode == "NotFound")
                return NotFound(new { message = errorMessage });
            return BadRequest(new { message = errorMessage });
        }

        return NoContent();
    }
}
