using System;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class PasswordResetTokenRepository : BaseRepository<PasswordResetToken>, IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(AppDbContext context) : base(context) { }

    public async Task<PasswordResetToken?> GetValidTokenAsync(Guid userId, string token)
    {
        return await _context.PasswordResetTokens
            .FirstOrDefaultAsync(prt => prt.UserId == userId && prt.Token == token && !prt.IsUsed && prt.Expiration > DateTime.UtcNow);
    }
}
