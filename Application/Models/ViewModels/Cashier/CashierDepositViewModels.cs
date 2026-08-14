using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Cashier; 

public class DepositViewModel
{
    [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta debe contener 9 dígitos.")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "El número de cuenta solo debe contener números.")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a depositar es requerido.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El monto a depositar debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}

public class ConfirmDepositViewModel
{
    [Required(ErrorMessage = "El número de cuenta es requerido.")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El titular de la cuenta es requerido.")]
    public string OwnerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto es requerido.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El monto debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}