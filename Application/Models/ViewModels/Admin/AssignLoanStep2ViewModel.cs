using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Admin;

public class AssignLoanStep2ViewModel
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientCedula { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a prestar es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a prestar debe ser mayor que cero.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "La tasa de interés anual es requerida.")]
    [Range(0.0, double.MaxValue, ErrorMessage = "La tasa de interés anual no puede ser negativa.")]
    public decimal InterestRate { get; set; }

    [Required(ErrorMessage = "El plazo del préstamo es requerido.")]
    public int TermInMonths { get; set; }
}