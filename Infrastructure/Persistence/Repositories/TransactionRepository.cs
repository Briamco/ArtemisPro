using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
{
    public TransactionRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Transaction>> GetBySavingsAccountIdAsync(Guid savingsAccountId)
    {
        return await _context.Transactions
            .Where(t => t.SavingsAccountId == savingsAccountId)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }
}
