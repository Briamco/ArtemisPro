using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class BeneficiaryRepository : BaseRepository<Beneficiary>, IBeneficiaryRepository
{
    public BeneficiaryRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Beneficiary>> GetByClientIdAsync(Guid clientId)
    {
        return await _context.Beneficiaries
            .Where(b => b.ClientId == clientId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<Beneficiary?> GetByClientAndAccountAsync(Guid clientId, string accountNumber)
    {
        return await _context.Beneficiaries
            .FirstOrDefaultAsync(b => b.ClientId == clientId && b.BeneficiaryAccountNumber == accountNumber);
    }
}
