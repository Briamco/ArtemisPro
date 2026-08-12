using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class CreateBeneficiaryDto
{
    [Required(ErrorMessage = "El número de cuenta del beneficiario es requerido.")]
    public string BeneficiaryAccountNumber { get; set; } = string.Empty;

    public string Alias { get; set; } = string.Empty;
}
