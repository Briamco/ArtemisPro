using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class LoanInstallmentRepository : BaseRepository<LoanInstallment>, ILoanInstallmentRepository
{
    public LoanInstallmentRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<LoanInstallment>> GetByLoanIdAsync(Guid loanId)
    {
        return await _context.LoanInstallments
            .Where(li => li.LoanId == loanId)
            .OrderBy(li => li.InstallmentNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<LoanInstallment>> GetOverdueInstallmentsAsync()
    {
        return await _context.LoanInstallments
            .Include(li => li.Loan)
            .Where(li => li.IsOverdue && li.PaymentStatus != Domain.Enums.PaymentStatus.Pagada)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalPendingDebtByClientIdAsync(Guid clientId)
    {
        var activeLoanIds = _context.Loans
            .Where(l => l.ClientId == clientId && l.Status == Domain.Enums.LoanStatus.Activo)
            .Select(l => l.Id);
            
        return await _context.LoanInstallments
            .Where(i => activeLoanIds.Contains(i.LoanId) && i.PaymentStatus != Domain.Enums.PaymentStatus.Pagada)
            .SumAsync(i => (decimal?)i.PendingBalance) ?? 0;
    }

    public async Task<decimal> GetTotalSystemPendingDebtAsync()
    {
        var activeLoanIds = _context.Loans
            .Where(l => l.Status == Domain.Enums.LoanStatus.Activo)
            .Select(l => l.Id);
            
        return await _context.LoanInstallments
            .Where(i => activeLoanIds.Contains(i.LoanId) && i.PaymentStatus != Domain.Enums.PaymentStatus.Pagada)
            .SumAsync(i => (decimal?)i.PendingBalance) ?? 0;
    }
}
