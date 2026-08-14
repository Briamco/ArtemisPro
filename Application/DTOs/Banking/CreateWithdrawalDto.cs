using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class CreateWithdrawalDto
{
    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a retirar es requerido.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El monto a retirar debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}
