using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class UpdateLoanRateDto
{
    [Required(ErrorMessage = "La tasa de interés anual es requerida.")]
    [Range(0, double.MaxValue, ErrorMessage = "La tasa de interés anual no puede ser negativa.")]
    public decimal AnnualInterestRate { get; set; }
}
