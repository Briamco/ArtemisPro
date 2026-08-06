using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Admin;

public class EditLoanRateViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "La tasa de interés anual es requerida.")]
    [Range(0.0, double.MaxValue, ErrorMessage = "La tasa de interés anual no puede ser negativa.")]
    public decimal InterestRate { get; set; }
}