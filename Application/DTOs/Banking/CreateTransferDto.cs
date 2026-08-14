using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class CreateTransferDto
{
    [Required(ErrorMessage = "La cuenta de origen es requerida.")]
    public Guid OriginAccountId { get; set; }

    [Required(ErrorMessage = "La cuenta de destino es requerida.")]
    public Guid DestinationAccountId { get; set; }

    [Required(ErrorMessage = "El monto a transferir es requerido.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}
