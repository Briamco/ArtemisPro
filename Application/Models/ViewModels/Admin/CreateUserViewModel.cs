using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Admin;

public class CreateUserViewModel
{
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

    [Required(ErrorMessage = "El tipo de usuario es requerido.")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de contraseña es requerida.")]
    [Compare("Password", ErrorMessage = "La contraseña y la confirmación de contraseña deben coincidir.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "El monto inicial no puede ser negativo.")]
    public decimal? InitialAmount { get; set; }
}