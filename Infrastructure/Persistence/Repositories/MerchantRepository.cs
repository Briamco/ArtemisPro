using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class MerchantRepository : BaseRepository<Merchant>, IMerchantRepository
{
    public MerchantRepository(AppDbContext context) : base(context) { }

    public async Task<Merchant?> GetByRNCAsync(string rnc)
    {
        return await _context.Merchants
            .Include(m => m.Users)
            .FirstOrDefaultAsync(m => m.RNC == rnc);
    }

    public async Task<Merchant?> GetByEmailAsync(string email)
    {
        return await _context.Merchants
            .Include(m => m.Users)
            .FirstOrDefaultAsync(m => m.Email == email);
    }

    public async Task<Merchant?> GetByIdWithUsersAsync(Guid id)
    {
        return await _context.Merchants
            .Include(m => m.Users)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<(List<Merchant> Items, int TotalRecords)> GetPagedAsync(int page, int pageSize, string? status)
    {
        var query = _context.Merchants
            .Include(m => m.Users)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("activo", StringComparison.OrdinalIgnoreCase))
                query = query.Where(m => m.Status == MerchantStatus.Activo);
            else if (status.Equals("inactivo", StringComparison.OrdinalIgnoreCase))
                query = query.Where(m => m.Status == MerchantStatus.Inactivo);
            // "todos" leaves query unfiltered
        }
        else
        {
            // By default, show active merchants
            query = query.Where(m => m.Status == MerchantStatus.Activo);
        }

        var totalRecords = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalRecords);
    }
}
