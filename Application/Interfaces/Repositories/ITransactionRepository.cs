using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ITransactionRepository : IBaseRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetBySavingsAccountIdAsync(Guid savingsAccountId);
}
