using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Cashier;

public class ThirdPartyTransferViewModel
{
    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    public string SourceAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
    public string DestinationAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto de la transacción es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto de la transacción debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}

public class ConfirmThirdPartyTransferViewModel
{
    public string SourceAccountOwner { get; set; } = string.Empty;
    public string SourceAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountOwner { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}