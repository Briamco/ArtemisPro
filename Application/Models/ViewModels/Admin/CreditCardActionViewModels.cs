using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Admin;

public class CreditCardDetailsViewModel
{
    public string MaskedNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ExpirationDate { get; set; } = string.Empty;
    public string CardType { get; set; } = "ARTEMIS ELITE";
    public List<ConsumptionViewModel> Consumptions { get; set; } = new();
}

public class ConsumptionViewModel
{
    public DateTime Date { get; set; }
    public string Commerce { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty; 
}

public class EditCreditCardLimitViewModel
{
    public string Id { get; set; } = string.Empty;
    public string MaskedNumber { get; set; } = string.Empty;
    public decimal CurrentDebt { get; set; }
    public decimal CurrentLimit { get; set; }

    [Required(ErrorMessage = "El límite de la tarjeta es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El límite de la tarjeta debe ser mayor que cero.")]
    public decimal NewLimit { get; set; }
}

public class CancelCreditCardViewModel
{
    public string Id { get; set; } = string.Empty;
    public string MaskedNumber { get; set; } = string.Empty;
    public decimal CurrentDebt { get; set; }
}