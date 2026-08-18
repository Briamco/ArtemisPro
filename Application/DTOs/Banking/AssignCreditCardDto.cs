using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.DTOs.Banking;

public class AssignCreditCardDto
{
    [Required(ErrorMessage = "El cliente es requerido.")]
    public Guid ClientId { get; set; }

    [Required(ErrorMessage = "El límite de crédito es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El límite de crédito debe ser mayor que cero.")]
    [JsonPropertyName("creditLimit")]
    public decimal Limit { get; set; }
}
