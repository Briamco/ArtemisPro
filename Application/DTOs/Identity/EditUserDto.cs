using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Identity;

public class EditUserDto
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

    public string? NewPassword { get; set; }

    public string? ConfirmPassword { get; set; }

    public decimal AdditionalAmount { get; set; }
}
