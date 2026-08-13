using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Cashier;

public class WithdrawalViewModel
{
    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a retirar es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a retirar debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}

public class ConfirmWithdrawalViewModel
{
    public string AccountNumber { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}