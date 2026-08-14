using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Identity;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using AutoMapper;
using System.Security.Cryptography;

namespace Application.Services;

public class UserAppService : IUserAppService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuthAppService _authAppService;

    public UserAppService(
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAuthAppService authAppService)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _authAppService = authAppService;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync(string? role = null)
    {
        IEnumerable<ApplicationUser> users;
        
        if (!string.IsNullOrEmpty(role))
        {
            users = await _userManager.GetUsersInRoleAsync(role);
        }
        else
        {
            users = await _unitOfWork.Users.GetAllAsync();
        }

        var dtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var dto = _mapper.Map<UserDto>(user);
            dto.Role = roles.FirstOrDefault() ?? string.Empty;
            dtos.Add(dto);
        }

        return dtos.OrderByDescending(u => u.Id);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var dto = _mapper.Map<UserDto>(user);
        dto.Role = roles.FirstOrDefault() ?? string.Empty;
        
        return dto;
    }

    public async Task<(bool Success, string? Error)> CreateUserAsync(CreateUserDto dto, string confirmationLinkFormat)
    {
        // Validaciones
        var existingCedula = await _unitOfWork.Users.FindAsync(u => u.Cedula == dto.Cedula);
        if (existingCedula.Any())
            return (false, "Ya existe un usuario registrado con esta cédula.");

        var existingEmail = await _userManager.FindByEmailAsync(dto.Email);
        if (existingEmail != null)
            return (false, "Ya existe un usuario registrado con este correo electrónico.");

        var existingUsername = await _userManager.FindByNameAsync(dto.UserName);
        if (existingUsername != null)
            return (false, "Ya existe un usuario registrado con este nombre de usuario.");

        if (dto.Role == "Cliente" && dto.InitialBalance < 0)
            return (false, "El monto inicial no puede ser negativo.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Delegar creación y envío de correo a AuthAppService
            var result = await _authAppService.RegisterAsync(dto, confirmationLinkFormat);
            if (!result.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // Creación automática de cuenta principal para clientes
            if (dto.Role == "Cliente")
            {
                var createdUser = await _userManager.FindByEmailAsync(dto.Email);
                if (createdUser != null)
                {
                    string accountNumber = await GenerateUniqueAccountNumberAsync();
                    
                    var account = new SavingsAccount
                    {
                        ClientId = createdUser.Id,
                        AccountNumber = accountNumber,
                        Balance = dto.InitialBalance,
                        AccountType = AccountType.Principal,
                        Status = AccountStatus.Activa,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _unitOfWork.SavingsAccounts.AddAsync(account);

                    // Si monto inicial > 0, registrar transacción de Crédito
                    if (dto.InitialBalance > 0)
                    {
                        var transaction = new Transaction
                        {
                            SavingsAccountId = account.Id,
                            Amount = dto.InitialBalance,
                            Type = TransactionType.CRÉDITO,
                            Beneficiary = $"{dto.FirstName} {dto.LastName}",
                            Origin = "Apertura de cuenta",
                            Status = TransactionStatus.APROBADA,
                            Date = DateTime.UtcNow
                        };
                        await _unitOfWork.Transactions.AddAsync(transaction);
                    }
                    
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            await _unitOfWork.CommitTransactionAsync();
            return (true, null);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<(bool Success, string? Error)> EditUserAsync(Guid id, EditUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return (false, "El usuario seleccionado no existe.");

        var existingCedula = await _unitOfWork.Users.FindAsync(u => u.Cedula == dto.Cedula && u.Id != id);
        if (existingCedula.Any())
            return (false, "Ya existe otro usuario registrado con esta cédula.");

        var existingEmail = await _userManager.FindByEmailAsync(dto.Email);
        if (existingEmail != null && existingEmail.Id != id)
            return (false, "Ya existe otro usuario registrado con este correo electrónico.");

        var existingUsername = await _userManager.FindByNameAsync(dto.UserName);
        if (existingUsername != null && existingUsername.Id != id)
            return (false, "Ya existe otro usuario registrado con este nombre de usuario.");

        if (dto.AdditionalAmount < 0)
            return (false, "El monto adicional no puede ser negativo.");

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Cedula = dto.Cedula;
        user.Email = dto.Email;
        user.UserName = dto.UserName;

        if (!string.IsNullOrEmpty(dto.NewPassword))
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return (false, "La contraseña y la confirmación deben coincidir.");
            
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
            if (!resetResult.Succeeded)
                return (false, "Error al actualizar la contraseña.");
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return (false, "Error al actualizar el usuario.");

        // Si es cliente y hay monto adicional, actualizar balance y registrar transacción
        var isClient = await _userManager.IsInRoleAsync(user, "Cliente");
        if (isClient && dto.AdditionalAmount > 0)
        {
            var accounts = await _unitOfWork.SavingsAccounts.FindAsync(a => a.ClientId == user.Id && a.AccountType == AccountType.Principal);
            var mainAccount = accounts.FirstOrDefault();
            
            if (mainAccount != null)
            {
                mainAccount.Balance += dto.AdditionalAmount;
                _unitOfWork.SavingsAccounts.Update(mainAccount);

                var transaction = new Transaction
                {
                    SavingsAccountId = mainAccount.Id,
                    Amount = dto.AdditionalAmount,
                    Type = TransactionType.CRÉDITO,
                    Beneficiary = $"{user.FirstName} {user.LastName}",
                    Origin = "Abono adicional",
                    Status = TransactionStatus.APROBADA,
                    Date = DateTime.UtcNow
                };
                await _unitOfWork.Transactions.AddAsync(transaction);
                
                await _unitOfWork.SaveChangesAsync();
            }
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ToggleUserStatusAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return (false, "Usuario no encontrado.");

        user.IsActive = !user.IsActive;
        
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, "Error al actualizar el estado del usuario.");
            
        return (true, null);
    }

    private async Task<string> GenerateUniqueAccountNumberAsync()
    {
        string accountNumber;
        bool existsInSavings;
        bool existsInLoans;
        
        do
        {
            accountNumber = RandomNumberGenerator.GetInt32(100000000, 999999999).ToString();
            
            existsInSavings = await _unitOfWork.SavingsAccounts.ExistsAsync(a => a.AccountNumber == accountNumber);
            existsInLoans = await _unitOfWork.Loans.ExistsAsync(l => l.LoanNumber == accountNumber);
            
        } while (existsInSavings || existsInLoans);

        return accountNumber;
    }
}
