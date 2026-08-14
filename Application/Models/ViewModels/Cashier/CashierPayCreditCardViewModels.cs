using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Cashier;

public class PayCreditCardViewModel
{
    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta origen debe contener 9 dígitos.")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "El número de cuenta origen solo debe contener números.")]
    public string SourceAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de tarjeta de crédito es requerido.")]
    [StringLength(16, MinimumLength = 16, ErrorMessage = "El número de tarjeta debe contener 16 dígitos.")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "El número de tarjeta solo debe contener números.")]
    public string CreditCardNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a pagar es requerido.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}

public class ConfirmPayCreditCardViewModel
{
    [Required(ErrorMessage = "El titular de la cuenta origen es requerido.")]
    public string SourceAccountOwner { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    public string SourceAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El titular de la tarjeta es requerido.")]
    public string CreditCardOwner { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de tarjeta es requerido.")]
    public string CreditCardMasked { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto ingresado es requerido.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El monto ingresado debe ser mayor que cero.")]
    public decimal EnteredAmount { get; set; }

    [Required(ErrorMessage = "El monto efectivo es requerido.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El monto efectivo debe ser mayor que cero.")]
    public decimal EffectiveAmount { get; set; }
}