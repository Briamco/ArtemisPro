using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Services;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class LoanController : ControllerBase
    {
        private readonly ILoanAppService _loanAppService;

        public LoanController(ILoanAppService loanAppService)
        {
            _loanAppService = loanAppService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLoans(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? identification = null)
        {
            var result = await _loanAppService.GetLoansAsync(page, pageSize, status, identification);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLoanById(Guid id)
        {
            var loan = await _loanAppService.GetLoanByIdAsync(id);
            if (loan == null) return NotFound();
            return Ok(loan);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLoan([FromBody] CreateLoanDto dto)
        {
            var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminIdClaim) || !Guid.TryParse(adminIdClaim, out var adminId))
                return Unauthorized(new { message = "No se pudo identificar el administrador autenticado." });

            try
            {
                var result = await _loanAppService.CreateLoanAsync(dto, adminId);
                return StatusCode(201, result);
            }
            catch (HighRiskConflictException ex)
            {
                return Conflict(new
                {
                    message = ex.Message,
                    riskType = ex.RiskType,
                    currentDebt = ex.CurrentDebt,
                    projectedDebt = ex.ProjectedDebt,
                    averageDebt = ex.AverageDebt
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/rate")]
        public async Task<IActionResult> UpdateLoanRate(Guid id, [FromBody] UpdateLoanRateDto dto)
        {
            var (success, error) = await _loanAppService.UpdateLoanRateAsync(id, dto);
            if (!success) return BadRequest(new { message = error });
            return NoContent();
        }
    }
}
