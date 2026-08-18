using System;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class CreditCardController : ControllerBase
{
    private readonly ICreditCardAppService _creditCardAppService;

    public CreditCardController(ICreditCardAppService creditCardAppService)
    {
        _creditCardAppService = creditCardAppService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCreditCards(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? identification = null)
    {
        if (pageSize > 20) pageSize = 20;
        if (page < 1) page = 1;

        var result = await _creditCardAppService.GetCreditCardsPagedAsync(page, pageSize, status, identification);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCreditCardById(Guid id)
    {
        var card = await _creditCardAppService.GetCreditCardDetailByIdAsync(id);
        if (card == null) return NotFound(new { message = "Tarjeta no encontrada." });
        return Ok(card);
    }

    [HttpPost]
    public async Task<IActionResult> AssignCreditCard([FromBody] AssignCreditCardDto dto)
    {
        var (success, error, card) = await _creditCardAppService.AssignCreditCardAsync(dto);
        if (!success) return BadRequest(new { message = error });
        return StatusCode(201, card);
    }

    [HttpPatch("{id}/limit")]
    public async Task<IActionResult> UpdateCreditCardLimit(Guid id, [FromBody] UpdateCreditCardLimitDto dto)
    {
        var (success, error) = await _creditCardAppService.UpdateCreditCardLimitAsync(id, dto);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> CancelCreditCard(Guid id)
    {
        var (success, error) = await _creditCardAppService.CancelCreditCardAsync(id);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }
}
