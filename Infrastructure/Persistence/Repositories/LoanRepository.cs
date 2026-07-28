using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class LoanRepository : BaseRepository<Loan>, ILoanRepository
{
    public LoanRepository(AppDbContext context) : base(context) { }

    public async Task<Loan?> GetByLoanNumberAsync(string loanNumber)
    {
        return await _context.Loans
            .Include(l => l.Client)
            .FirstOrDefaultAsync(l => l.LoanNumber == loanNumber);
    }

    public async Task<Loan?> GetActiveByClientIdAsync(Guid clientId)
    {
        return await _context.Loans
            .FirstOrDefaultAsync(l => l.ClientId == clientId && l.Status == Domain.Enums.LoanStatus.Activo);
    }

    public async Task<IEnumerable<Loan>> GetByClientIdAsync(Guid clientId)
    {
        return await _context.Loans
            .Include(l => l.Client)
            .Where(l => l.ClientId == clientId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }
}
