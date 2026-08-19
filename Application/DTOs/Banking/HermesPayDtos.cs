using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class ProcessPaymentDto
{
    [Required(ErrorMessage = "El número de tarjeta es requerido.")]
    public string CardNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El mes de expiración es requerido.")]
    public string MonthExpirationCard { get; set; } = string.Empty;

    [Required(ErrorMessage = "El año de expiración es requerido.")]
    public string YearExpirationCard { get; set; } = string.Empty;

    [Required(ErrorMessage = "El CVC es requerido.")]
    public string Cvc { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto de la transacción es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto de la transacción debe ser mayor que cero.")]
    public decimal TransactionAmount { get; set; }
}

public class CommerceTransactionsResponseDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public string CommerceId { get; set; } = string.Empty;
    public string CommerceName { get; set; } = string.Empty;
    public List<CommerceTransactionItemDto> Data { get; set; } = new();
}

public class CommerceTransactionItemDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string CardLastFourDigits { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
