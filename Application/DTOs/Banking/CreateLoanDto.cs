using System;
using System.ComponentModel.DataAnnotations;
using Application.DTOs.Banking;

namespace Application.DTOs.Banking;

public class CreateLoanDto
{
    [Required(ErrorMessage = "El cliente es requerido.")]
    public Guid ClientId { get; set; }

    [Required(ErrorMessage = "El plazo del prestamo es requerido.")]
    [AllowedTerms]
    public int TermInMonths { get; set; }

    [Required(ErrorMessage = "El monto a prestar es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a prestar debe ser mayor que cero.")]
    public decimal CapitalAmount { get; set; }

    [Required(ErrorMessage = "La tasa de interes anual es requerida.")]
    [Range(0, double.MaxValue, ErrorMessage = "La tasa de interes anual no puede ser negativa.")]
    public decimal AnnualInterestRate { get; set; }

    public bool ConfirmHighRisk { get; set; } = false;
}
