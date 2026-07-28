using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Identity;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "El nombre de usuario es requerido.")]
    public string UserName { get; set; } = string.Empty;
}
