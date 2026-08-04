using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Account 
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "El nombre de usuario es requerido.")]
        public string UserName { get; set; } = string.Empty;
    }
}