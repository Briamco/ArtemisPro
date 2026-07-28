using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class CreateLoanDto
{
    [Required(ErrorMessage = "El cliente es requerido.")]
    public Guid ClientId { get; set; }

    [Required(ErrorMessage = "El plazo del préstamo es requerido.")]
    public int Term { get; set; }

    [Required(ErrorMessage = "El monto a prestar es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a prestar debe ser mayor que cero.")]
    public decimal ApprovedAmount { get; set; }

    [Required(ErrorMessage = "La tasa de interés anual es requerida.")]
    [Range(0, double.MaxValue, ErrorMessage = "La tasa de interés anual no puede ser negativa.")]
    public decimal AnnualInterestRate { get; set; }
}
