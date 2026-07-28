using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class CreateBeneficiaryDto
{
    [Required(ErrorMessage = "El número de cuenta del beneficiario es requerido.")]
    public string BeneficiaryAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El alias es requerido.")]
    public string Alias { get; set; } = string.Empty;
}
