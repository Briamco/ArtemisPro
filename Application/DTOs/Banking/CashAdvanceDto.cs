using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class CashAdvanceDto
{
    [Required]
    public Guid ClientId { get; set; }

    [Required]
    public Guid CreditCardId { get; set; }

    [Required]
    public Guid DestinationAccountId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "El monto del avance debe ser mayor que cero.")]
    public decimal Amount { get; set; }
}
