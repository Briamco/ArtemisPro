using System;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ISavingsAccountRepository : IBaseRepository<SavingsAccount>
{
    Task<SavingsAccount?> GetByAccountNumberAsync(string accountNumber);
    Task<SavingsAccount?> GetPrimaryByClientIdAsync(Guid clientId);
    Task<IEnumerable<SavingsAccount>> GetByClientIdAsync(Guid clientId);
}
