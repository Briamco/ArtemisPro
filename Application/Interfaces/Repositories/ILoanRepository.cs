using System;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ILoanRepository : IBaseRepository<Loan>
{
    Task<Loan?> GetByLoanNumberAsync(string loanNumber);
    Task<Loan?> GetActiveByClientIdAsync(Guid clientId);
    Task<IEnumerable<Loan>> GetByClientIdAsync(Guid clientId);
}
