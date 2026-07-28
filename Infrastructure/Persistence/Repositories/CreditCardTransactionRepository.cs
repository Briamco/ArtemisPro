using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class CreditCardTransactionRepository : BaseRepository<CreditCardTransaction>, ICreditCardTransactionRepository
{
    public CreditCardTransactionRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<CreditCardTransaction>> GetByCreditCardIdAsync(Guid creditCardId)
    {
        return await _context.CreditCardTransactions
            .Where(cct => cct.CreditCardId == creditCardId)
            .OrderByDescending(cct => cct.Date)
            .ToListAsync();
    }
}
