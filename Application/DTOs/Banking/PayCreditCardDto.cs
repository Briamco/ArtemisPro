using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class PayCreditCardDto
{
    [Required(ErrorMessage = "La cuenta de origen es requerida.")]
    public Guid SourceAccountId { get; set; }

    [Required(ErrorMessage = "La tarjeta de crédito destino es requerida.")]
    public Guid CreditCardId { get; set; }

    [Required(ErrorMessage = "El cliente es requerido.")]
    public Guid ClientId { get; set; }

    [Required(ErrorMessage = "El monto a pagar es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}
