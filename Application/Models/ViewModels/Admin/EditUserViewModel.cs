using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Admin;

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es requerido.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cédula es requerida.")]
    public string Identification { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es requerido.")]
    [EmailAddress(ErrorMessage = "El correo electrónico debe tener un formato válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es requerido.")]
    public string Username { get; set; } = string.Empty;
    
    public string Role { get; set; } = string.Empty; 

    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [Compare("NewPassword", ErrorMessage = "La contraseña y la confirmación de contraseña deben coincidir.")]
    [DataType(DataType.Password)]
    public string? ConfirmNewPassword { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El monto adicional no puede ser negativo.")]
    public decimal? AdditionalAmount { get; set; }
}