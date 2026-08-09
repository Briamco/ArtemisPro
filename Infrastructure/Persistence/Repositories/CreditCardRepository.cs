using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class CreditCardRepository : BaseRepository<CreditCard>, ICreditCardRepository
{
    public CreditCardRepository(AppDbContext context) : base(context) { }

    public async Task<CreditCard?> GetByCardNumberAsync(string cardNumber)
    {
        return await _context.CreditCards
            .Include(cc => cc.Client)
            .FirstOrDefaultAsync(cc => cc.CardNumber == cardNumber);
    }

    public async Task<IEnumerable<CreditCard>> GetByClientIdAsync(Guid clientId)
    {
        return await _context.CreditCards
            .Include(cc => cc.Client)
            .Where(cc => cc.ClientId == clientId)
            .OrderByDescending(cc => cc.CreatedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalActiveDebtByClientIdAsync(Guid clientId)
    {
        return await _context.CreditCards
            .Where(c => c.ClientId == clientId && c.Status == Domain.Enums.CardStatus.Activa)
            .SumAsync(c => (decimal?)c.Debt) ?? 0;
    }

    public async Task<decimal> GetTotalSystemActiveDebtAsync()
    {
        return await _context.CreditCards
            .Where(c => c.Status == Domain.Enums.CardStatus.Activa)
            .SumAsync(c => (decimal?)c.Debt) ?? 0;
    }
}
