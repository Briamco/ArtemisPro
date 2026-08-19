using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Features.Commerce.Commands;
using Application.Features.Commerce.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class CommerceController : ControllerBase
{
    private readonly ISender _sender;

    public CommerceController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetCommerces(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        if (page < 1 || pageSize < 1)
            return BadRequest(new { message = "Los parámetros de paginación deben ser mayores que cero." });

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.ToLowerInvariant();
            if (s != "activo" && s != "inactivo" && s != "todos")
                return BadRequest(new { message = "El estado filtrado solo puede ser activo, inactivo o todos." });
        }

        var result = await _sender.Send(new GetCommercesPagedQuery(page, pageSize, status));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCommerceById(Guid id)
    {
        var result = await _sender.Send(new GetCommerceByIdQuery(id));
        if (result == null)
            return NotFound(new { message = "El comercio indicado no existe." });

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCommerce([FromBody] CreateCommerceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? adminId = null;
        if (!string.IsNullOrEmpty(adminIdClaim) && Guid.TryParse(adminIdClaim, out var parsedAdminId))
            adminId = parsedAdminId;

        var (success, errorCode, errorMessage, commerce) = await _sender.Send(new CreateCommerceCommand(dto, adminId));
        if (!success)
        {
            if (errorCode == "Conflict")
                return Conflict(new { message = errorMessage });
            return BadRequest(new { message = errorMessage });
        }

        return StatusCode(201, commerce);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCommerce(Guid id, [FromBody] UpdateCommerceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, errorCode, errorMessage) = await _sender.Send(new UpdateCommerceCommand(id, dto));
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
    public async Task<IActionResult> UpdateCommerceStatus(Guid id, [FromBody] UpdateCommerceStatusDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, errorCode, errorMessage) = await _sender.Send(new UpdateCommerceStatusCommand(id, dto.Status));
        if (!success)
        {
            if (errorCode == "NotFound")
                return NotFound(new { message = errorMessage });
            return BadRequest(new { message = errorMessage });
        }

        return NoContent();
    }
}
