using System;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Seeds;

public static class DefaultRolesAndUsers
{
    public static async Task SeedAsync(AppDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        var roles = new[] { "Administrador", "Cajero", "Cliente", "Comercio" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role, NormalizedName = role.ToUpper() });
            }
        }

        if (!context.Users.Any())
        {
            var users = new (string FirstName, string LastName, string Cedula, string Email, string UserName, string Password, string Role)[]
            {
                ("Admin", "Sistema", "001-0000000-0", "admin@artemisbanking.com", "admin", "Admin@123!", "Administrador"),
                ("Cajero", "Principal", "002-0000000-0", "cajero@artemisbanking.com", "cajero", "Cajero@123!", "Cajero"),
                ("Cliente", "Default", "003-0000000-0", "cliente@artemisbanking.com", "cliente", "Cliente@123!", "Cliente"),
                ("Comercio", "Principal", "004-0000000-0", "comercio@artemisbanking.com", "comercio", "Comercio@123!", "Comercio")
            };

            foreach (var u in users)
            {
                var user = new ApplicationUser
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Cedula = u.Cedula,
                    Email = u.Email,
                    UserName = u.UserName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, u.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, u.Role);
                }
            }
        }
    }
}
