using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ICreditCardTransactionRepository : IBaseRepository<CreditCardTransaction>
{
    Task<IEnumerable<CreditCardTransaction>> GetByCreditCardIdAsync(Guid creditCardId);
}
