using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Repositories;

public interface ICreditCardRepository : IBaseRepository<CreditCard>
{
    Task<CreditCard?> GetByCardNumberAsync(string cardNumber);
    Task<IEnumerable<CreditCard>> GetByClientIdAsync(Guid clientId);
    Task<decimal> GetTotalActiveDebtByClientIdAsync(Guid clientId);
    Task<decimal> GetTotalSystemActiveDebtAsync();
    Task<(List<CreditCard> Cards, int TotalCount)> GetPagedAsync(int page, int pageSize, CardStatus? status, string? clientCedula);
}
