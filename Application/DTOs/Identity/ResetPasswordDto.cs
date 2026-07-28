using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Identity;

public class ResetPasswordDto
{
    [Required(ErrorMessage = "El token es requerido.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de contraseña es requerida.")]
    [Compare("NewPassword", ErrorMessage = "La contraseña y la confirmación de contraseña deben coincidir.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
