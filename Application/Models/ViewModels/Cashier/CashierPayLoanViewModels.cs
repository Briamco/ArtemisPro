using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Cashier;

public class PayLoanViewModel
{
    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    public string SourceAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número del préstamo es requerido.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "El número del préstamo debe contener 9 dígitos.")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "El número del préstamo solo debe contener números.")]
    public string LoanNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a pagar es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}

public class ConfirmPayLoanViewModel
{
    public string SourceAccountOwner { get; set; } = string.Empty;
    public string SourceAccountNumber { get; set; } = string.Empty;
    public string LoanOwner { get; set; } = string.Empty;
    public string LoanNumber { get; set; } = string.Empty;
    public decimal EnteredAmount { get; set; }
    public decimal EffectiveAmount { get; set; }
}

// DTO for simulation of loans
public class CashierSystemLoanDto
{
    public string LoanNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; 
    public decimal PendingAmount { get; set; }
    public bool HasPendingInstallments { get; set; }
}