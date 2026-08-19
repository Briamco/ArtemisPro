using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Identity;

public class ConfirmAccountApiDto
{
    [Required(ErrorMessage = "El token es requerido.")]
    public string Token { get; set; } = string.Empty;
}

public class GetResetTokenApiDto
{
    [Required(ErrorMessage = "El nombre de usuario es requerido.")]
    public string UserName { get; set; } = string.Empty;
}

public class ResetPasswordApiDto
{
    [Required(ErrorMessage = "El identificador del usuario es requerido.")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El token es requerido.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de contraseña es requerida.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
