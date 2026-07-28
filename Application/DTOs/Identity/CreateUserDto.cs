using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Identity;

public class CreateUserDto
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cédula es requerida.")]
    public string Cedula { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es requerido.")]
    [EmailAddress(ErrorMessage = "El correo electrónico debe tener un formato válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es requerido.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de contraseña es requerida.")]
    [Compare("Password", ErrorMessage = "La contraseña y la confirmación de contraseña deben coincidir.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de usuario es requerido.")]
    public string Role { get; set; } = string.Empty;

    public decimal InitialBalance { get; set; }
}
