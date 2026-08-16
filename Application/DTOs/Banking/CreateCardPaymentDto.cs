using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class CreateCardPaymentDto
{
    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de tarjeta de crédito es requerido.")]
    [StringLength(16, MinimumLength = 16, ErrorMessage = "El número de tarjeta debe contener 16 dígitos.")]
    public string CardNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a pagar es requerido.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}
