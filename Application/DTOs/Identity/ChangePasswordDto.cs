using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Identity;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "La contraseña actual es requerida.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es requerida.")]
    [MinLength(8, ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de contraseña es requerida.")]
    [Compare("NewPassword", ErrorMessage = "La nueva contraseña y la confirmación deben coincidir.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
