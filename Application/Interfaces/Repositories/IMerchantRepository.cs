using System;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IMerchantRepository : IBaseRepository<Merchant>
{
    Task<Merchant?> GetByRNCAsync(string rnc);
    Task<Merchant?> GetByEmailAsync(string email);
    Task<Merchant?> GetByIdWithUsersAsync(Guid id);
    Task<(List<Merchant> Items, int TotalRecords)> GetPagedAsync(int page, int pageSize, string? status);
}
