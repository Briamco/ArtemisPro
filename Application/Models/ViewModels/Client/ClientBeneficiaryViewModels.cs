using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Client;

public class BeneficiaryListViewModel
{
    public List<BeneficiaryViewModel> Beneficiaries { get; set; } = new();
}

public class BeneficiaryViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
}

public class AddBeneficiaryViewModel
{
    [Required(ErrorMessage = "El número de cuenta es requerido.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = "El número de cuenta debe contener exactamente 9 dígitos.")]
    public string AccountNumber { get; set; } = string.Empty;
}

public class DeleteBeneficiaryViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
}


public class SystemAccountDto
{
    public string AccountNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; 
    public string OwnerId { get; set; } = string.Empty; 
}