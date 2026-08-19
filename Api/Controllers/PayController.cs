using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Features.HermesPay.Commands;
using Application.Features.HermesPay.Queries;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("pay")]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Comercio")]
public class PayController : ControllerBase
{
    private readonly ISender _sender;
    private readonly UserManager<ApplicationUser> _userManager;

    public PayController(ISender sender, UserManager<ApplicationUser> userManager)
    {
        _sender = sender;
        _userManager = userManager;
    }

    [HttpGet("get-transactions/{commerceId}")]
    public async Task<IActionResult> GetTransactions(
        Guid commerceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1)
            return BadRequest(new { message = "Los parámetros de paginación deben ser mayores que cero." });

        var targetCommerceId = await ResolveCommerceIdAsync(commerceId);
        if (!targetCommerceId.HasValue)
            return StatusCode(403, new { message = "Acceso denegado. El usuario no tiene un comercio asociado." });

        var (success, errorCode, errorMessage, result) = await _sender.Send(new GetCommerceTransactionsQuery(targetCommerceId.Value, page, pageSize));
        if (!success)
        {
            if (errorCode == "NotFound")
                return NotFound(new { message = errorMessage });
            return BadRequest(new { message = errorMessage });
        }

        return Ok(result);
    }

    [HttpPost("process-payment/{commerceId}")]
    public async Task<IActionResult> ProcessPayment(
        Guid commerceId,
        [FromBody] ProcessPaymentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var targetCommerceId = await ResolveCommerceIdAsync(commerceId);
        if (!targetCommerceId.HasValue)
            return StatusCode(403, new { message = "Acceso denegado. El usuario no tiene un comercio asociado." });

        var (success, errorCode, errorMessage) = await _sender.Send(new ProcessPaymentCommand(targetCommerceId.Value, dto));
        if (!success)
        {
            if (errorCode == "NotFound")
                return NotFound(new { message = errorMessage });
            return BadRequest(new { message = errorMessage });
        }

        return NoContent();
    }

    private async Task<Guid?> ResolveCommerceIdAsync(Guid routeCommerceId)
    {
        if (User.IsInRole("Comercio"))
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return null;

            var user = await _userManager.FindByIdAsync(userIdClaim);
            if (user == null || !user.MerchantId.HasValue)
                return null;

            return user.MerchantId.Value;
        }

        return routeCommerceId;
    }
}
