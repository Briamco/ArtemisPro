using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Identity;

public class LoginDto
{
    [Required(ErrorMessage = "El nombre de usuario es requerido.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
