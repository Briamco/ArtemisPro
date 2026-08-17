using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

public interface ICreditCardAppService
{
    Task<IEnumerable<CreditCardDto>> GetCreditCardsAsync(string? status = null, string? cedula = null);
    Task<CreditCardDto?> GetCreditCardByIdAsync(Guid id);
    Task<IEnumerable<CreditCardTransactionDto>> GetTransactionsAsync(Guid cardId);
    Task<(bool Success, string? Error)> AssignCreditCardAsync(AssignCreditCardDto dto);
    Task<(bool Success, string? Error)> UpdateCreditCardLimitAsync(Guid id, UpdateCreditCardLimitDto dto);
    Task<(bool Success, string? Error)> CancelCreditCardAsync(Guid id);
    Task<IEnumerable<CreditCardDto>> GetClientCardsAsync(Guid clientId);
}
