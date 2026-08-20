using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class SavingsAccountRepository : BaseRepository<SavingsAccount>, ISavingsAccountRepository
{
    public SavingsAccountRepository(AppDbContext context) : base(context) { }

    public override async Task<SavingsAccount?> GetByIdAsync(Guid id)
    {
        return await _context.SavingsAccounts
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public override async Task<IEnumerable<SavingsAccount>> GetAllAsync()
    {
        return await _context.SavingsAccounts
            .Include(a => a.Client)
            .ToListAsync();
    }

    public async Task<SavingsAccount?> GetByAccountNumberAsync(string accountNumber)
    {
        return await _context.SavingsAccounts
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
    }

    public async Task<SavingsAccount?> GetPrimaryByClientIdAsync(Guid clientId)
    {
        return await _context.SavingsAccounts
            .FirstOrDefaultAsync(a => a.ClientId == clientId && a.AccountType == Domain.Enums.AccountType.Principal && a.Status == Domain.Enums.AccountStatus.Activa);
    }

    public async Task<IEnumerable<SavingsAccount>> GetByClientIdAsync(Guid clientId)
    {
        return await _context.SavingsAccounts
            .Include(a => a.Client)
            .Where(a => a.ClientId == clientId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
}
