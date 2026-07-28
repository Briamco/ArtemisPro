using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ILoanInstallmentRepository : IBaseRepository<LoanInstallment>
{
    Task<IEnumerable<LoanInstallment>> GetByLoanIdAsync(Guid loanId);
    Task<IEnumerable<LoanInstallment>> GetOverdueInstallmentsAsync();
}
