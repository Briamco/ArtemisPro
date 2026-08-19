using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class SavingsAccountApiDto
{
    public string Id { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientFullName { get; set; } = string.Empty;
    public string Identification { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateSavingsAccountApiDto
{
    [Required(ErrorMessage = "El identificador del cliente es requerido.")]
    public string ClientId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El balance inicial es requerido.")]
    [Range(0, double.MaxValue, ErrorMessage = "El balance inicial no puede ser negativo.")]
    public decimal InitialBalance { get; set; }
}

public class SavingsAccountDetailWithTransactionsApiDto
{
    public string AccountNumber { get; set; } = string.Empty;
    public string ClientFullName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public PagedResultDto<SavingsAccountTransactionItemApiDto> Transactions { get; set; } = new();
}

public class SavingsAccountTransactionItemApiDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Beneficiary { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
