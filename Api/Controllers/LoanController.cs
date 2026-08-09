using Application.DTOs.Banking;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoanController : ControllerBase
    {
        private readonly ILoanAppService _loanAppService;

        public LoanController(ILoanAppService loanAppService)
        {
            _loanAppService = loanAppService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLoans([FromQuery] string? status, [FromQuery] string? cedula)
        {
            var loans = await _loanAppService.GetLoansAsync(status, cedula);
            return Ok(loans);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLoanById(Guid id)
        {
            var loan = await _loanAppService.GetLoanByIdAsync(id);
            if (loan == null) return NotFound();
            return Ok(loan);
        }

        [HttpGet("{id}/installments")]
        public async Task<IActionResult> GetInstallments(Guid id)
        {
            var installments = await _loanAppService.GetInstallmentsAsync(id);
            return Ok(installments);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLoan([FromBody] CreateLoanDto dto)
        {
            var result = await _loanAppService.CreateLoanAsync(dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id}/rate")]
        public async Task<IActionResult> UpdateLoanRate(Guid id, [FromBody] UpdateLoanRateDto dto)
        {
            var (success, error) = await _loanAppService.UpdateLoanRateAsync(id, dto);
            if (!success) return BadRequest(new { Error = error });
            return Ok(new { Success = true });
        }
    }
}
