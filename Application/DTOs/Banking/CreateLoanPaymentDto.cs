using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class CreateLoanPaymentDto
{
    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número del préstamo es requerido.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "El número del préstamo debe contener 9 dígitos.")]
    public string LoanNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a pagar es requerido.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}
