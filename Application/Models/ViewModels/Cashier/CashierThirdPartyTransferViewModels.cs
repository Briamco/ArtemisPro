using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Cashier;

public class ThirdPartyTransferViewModel
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

public class ConfirmThirdPartyTransferViewModel
{
    [Required(ErrorMessage = "El titular de la cuenta origen es requerido.")]
    public string SourceAccountOwner { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    public string SourceAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El titular de la cuenta destino es requerido.")]
    public string DestinationAccountOwner { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
    public string DestinationAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto es requerido.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El monto debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}