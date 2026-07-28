using System;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IPasswordResetTokenRepository : IBaseRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> GetValidTokenAsync(Guid userId, string token);
}
