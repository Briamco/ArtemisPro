using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class UpdateCreditCardLimitDto
{
    [Required(ErrorMessage = "El límite de la tarjeta es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El límite de la tarjeta debe ser mayor que cero.")]
    public decimal NewLimit { get; set; }
}
