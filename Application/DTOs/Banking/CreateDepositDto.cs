using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class CreateDepositDto
{
    [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a depositar es requerido.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El monto a depositar debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}
