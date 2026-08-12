using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Client;

public class CashAdvanceViewModel
{
    [Required(ErrorMessage = "La tarjeta de crédito origen es requerida.")]
    public string CreditCardId { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cuenta de ahorro destino es requerida.")]
    public string AccountId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto del avance es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto del avance debe ser mayor que cero.")]
    public decimal Amount { get; set; }

    public List<ClientCardViewModel> MyActiveCards { get; set; } = new();
    public List<ClientAccountViewModel> MyActiveAccounts { get; set; } = new();
}