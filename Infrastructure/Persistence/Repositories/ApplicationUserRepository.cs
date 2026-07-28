using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class ApplicationUserRepository : BaseRepository<ApplicationUser>, IApplicationUserRepository
{
    public ApplicationUserRepository(AppDbContext context) : base(context) { }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<ApplicationUser?> GetByCedulaAsync(string cedula)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Cedula == cedula);
    }

    public async Task<ApplicationUser?> GetByUserNameAsync(string userName)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
    }
}
