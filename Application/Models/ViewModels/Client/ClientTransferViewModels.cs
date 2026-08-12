using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Client;

public class TransferViewModel
{
    [Required(ErrorMessage = "La cuenta de origen es requerida.")]
    public string SourceAccountId { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cuenta de destino es requerida.")]
    public string DestinationAccountId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a transferir es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
    public decimal Amount { get; set; }

    public List<ClientAccountViewModel> MyActiveAccounts { get; set; } = new();
}

public class ConfirmTransferViewModel
{
    public string SourceAccountId { get; set; } = string.Empty;
    public string DestinationAccountId { get; set; } = string.Empty;
    public string SourceAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}