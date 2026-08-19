using System;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Features.CreditCards.Commands;
using Application.Features.CreditCards.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/credit-card")]
[Authorize(Roles = "Administrador")]
public class CreditCardController : ControllerBase
{
    private readonly ISender _sender;

    public CreditCardController(ISender sender)
    {
        _sender = sender;
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

        var result = await _sender.Send(new GetCreditCardsPagedQuery(page, pageSize, status, identification));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCreditCardById(Guid id)
    {
        var card = await _sender.Send(new GetCreditCardDetailQuery(id));
        if (card == null) return NotFound(new { message = "Tarjeta no encontrada." });
        return Ok(card);
    }

    [HttpPost]
    public async Task<IActionResult> AssignCreditCard([FromBody] AssignCreditCardDto dto)
    {
        var (success, error, card) = await _sender.Send(new AssignCreditCardCommand(dto));
        if (!success)
        {
            if (error != null && error.Contains("no encontrad", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return StatusCode(201, card);
    }

    [HttpPatch("{id}/limit")]
    public async Task<IActionResult> UpdateCreditCardLimit(Guid id, [FromBody] UpdateCreditCardLimitDto dto)
    {
        var (success, error) = await _sender.Send(new UpdateCreditCardLimitCommand(id, dto));
        if (!success)
        {
            if (error != null && error.Contains("no encontrad", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return NoContent();
    }

    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> CancelCreditCard(Guid id)
    {
        var (success, error) = await _sender.Send(new CancelCreditCardCommand(id));
        if (!success)
        {
            if (error != null && error.Contains("no encontrad", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return NoContent();
    }
}
