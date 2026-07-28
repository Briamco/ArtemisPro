using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IBeneficiaryRepository : IBaseRepository<Beneficiary>
{
    Task<IEnumerable<Beneficiary>> GetByClientIdAsync(Guid clientId);
    Task<Beneficiary?> GetByClientAndAccountAsync(Guid clientId, string accountNumber);
}
