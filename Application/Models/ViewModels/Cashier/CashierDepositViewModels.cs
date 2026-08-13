using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Cashier; 

public class DepositViewModel
{
    [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a depositar es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a depositar debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}

public class ConfirmDepositViewModel
{
    public string AccountNumber { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class CashierSystemAccountDto
{
    public string AccountNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}