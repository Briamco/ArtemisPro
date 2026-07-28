using System;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IMerchantRepository : IBaseRepository<Merchant>
{
    Task<Merchant?> GetByRNCAsync(string rnc);
}
