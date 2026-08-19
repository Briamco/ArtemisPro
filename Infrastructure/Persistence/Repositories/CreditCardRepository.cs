using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
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
            .Where(c => c.ClientId == clientId && c.Status == CardStatus.Activa)
            .SumAsync(c => (decimal?)c.Debt) ?? 0;
    }

    public async Task<decimal> GetTotalSystemActiveDebtAsync()
    {
        return await _context.CreditCards
            .Where(c => c.Status == CardStatus.Activa)
            .SumAsync(c => (decimal?)c.Debt) ?? 0;
    }

    public async Task<(List<CreditCard> Cards, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CardStatus? status, string? clientCedula)
    {
        IQueryable<CreditCard> query = _context.CreditCards.Include(cc => cc.Client);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (!string.IsNullOrEmpty(clientCedula))
        {
            var userId = await _context.Users
                .Where(u => u.Cedula == clientCedula)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userId == Guid.Empty)
                return (new List<CreditCard>(), 0);

            query = query.Where(c => c.ClientId == userId);
        }

        var totalCount = await query.CountAsync();

        var cards = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (cards, totalCount);
    }
}
