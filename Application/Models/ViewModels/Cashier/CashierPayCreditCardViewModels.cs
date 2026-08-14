using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Cashier;

public class PayCreditCardViewModel
{
    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    public string SourceAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de tarjeta de crédito es requerido.")]
    [StringLength(16, MinimumLength = 16, ErrorMessage = "El número de tarjeta debe contener 16 dígitos.")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "El número de tarjeta solo debe contener números.")]
    public string CreditCardNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a pagar es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}

public class ConfirmPayCreditCardViewModel
{
    public string SourceAccountOwner { get; set; } = string.Empty;
    public string SourceAccountNumber { get; set; } = string.Empty;
    public string CreditCardOwner { get; set; } = string.Empty;
    public string CreditCardMasked { get; set; } = string.Empty;
    public decimal EnteredAmount { get; set; }
    public decimal EffectiveAmount { get; set; }
}

// DTO simulation for cards
public class CashierSystemCardDto
{
    public string CardNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Debt { get; set; }
}