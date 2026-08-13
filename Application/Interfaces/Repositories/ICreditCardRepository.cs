using System;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ICreditCardRepository : IBaseRepository<CreditCard>
{
    Task<CreditCard?> GetByCardNumberAsync(string cardNumber);
    Task<IEnumerable<CreditCard>> GetByClientIdAsync(Guid clientId);
    Task<decimal> GetTotalActiveDebtByClientIdAsync(Guid clientId);
    Task<decimal> GetTotalSystemActiveDebtAsync();
}
