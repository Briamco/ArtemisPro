using System;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IApplicationUserRepository : IBaseRepository<ApplicationUser>
{
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<ApplicationUser?> GetByCedulaAsync(string cedula);
    Task<ApplicationUser?> GetByUserNameAsync(string userName);
}
