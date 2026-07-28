using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class MerchantRepository : BaseRepository<Merchant>, IMerchantRepository
{
    public MerchantRepository(AppDbContext context) : base(context) { }

    public async Task<Merchant?> GetByRNCAsync(string rnc)
    {
        return await _context.Merchants.FirstOrDefaultAsync(m => m.RNC == rnc);
    }
}
