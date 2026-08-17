using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class CreateThirdPartyTransactionDto
{
    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta origen debe contener 9 dígitos.")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "El número de cuenta origen solo debe contener números.")]
    public string SourceAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta destino debe contener 9 dígitos.")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "El número de cuenta destino solo debe contener números.")]
    public string DestinationAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto de la transacción es requerido.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El monto de la transacción debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}
