using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class CreateSavingsAccountDto
{
    [Required(ErrorMessage = "El cliente es requerido.")]
    public Guid ClientId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El balance inicial no puede ser negativo.")]
    public decimal InitialBalance { get; set; }
}
