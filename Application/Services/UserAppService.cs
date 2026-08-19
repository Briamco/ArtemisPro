using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Banking;
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
        
        if (!string.IsNullOrEmpty(role) && role != "Todos")
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
            var primaryRole = roles.FirstOrDefault() ?? string.Empty;

            // Excluir rol Comercio de la gestión web
            if (primaryRole == "Comercio" || roles.Contains("Comercio"))
                continue;

            var dto = _mapper.Map<UserDto>(user);
            dto.Role = primaryRole;
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
                            Beneficiary = account.AccountNumber,
                            Origin = "DEPÓSITO",
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
                    Beneficiary = mainAccount.AccountNumber,
                    Origin = "DEPÓSITO",
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

    public async Task<PagedResultDto<UserApiDto>> GetUsersPagedApiAsync(int page, int pageSize, string? role)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 20) pageSize = 20;

        IEnumerable<ApplicationUser> users;
        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRole = role.ToLowerInvariant() switch
            {
                "administrador" => "Administrador",
                "cajero" => "Cajero",
                "cliente" => "Cliente",
                _ => role
            };
            users = await _userManager.GetUsersInRoleAsync(normalizedRole);
        }
        else
        {
            users = await _unitOfWork.Users.GetAllAsync();
        }

        var list = new List<UserApiDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Comercio"))
                continue;

            list.Add(new UserApiDto
            {
                Id = user.Id.ToString(),
                UserName = user.UserName ?? string.Empty,
                Identification = user.Cedula,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty,
                IsActive = user.IsActive
            });
        }

        var totalRecords = list.Count;
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        var data = list
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResultDto<UserApiDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages > 0 ? totalPages : 1,
            Data = data
        };
    }

    public async Task<PagedResultDto<CommerceUserApiDto>> GetCommerceUsersPagedApiAsync(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 20) pageSize = 20;

        var users = await _userManager.GetUsersInRoleAsync("Comercio");
        var list = new List<CommerceUserApiDto>();

        foreach (var user in users)
        {
            string commerceName = string.Empty;
            string commerceIdStr = string.Empty;

            if (user.MerchantId.HasValue)
            {
                var merchant = await _unitOfWork.Merchants.GetByIdAsync(user.MerchantId.Value);
                if (merchant != null)
                {
                    commerceName = merchant.Name;
                    commerceIdStr = merchant.Id.ToString();
                }
            }

            list.Add(new CommerceUserApiDto
            {
                Id = user.Id.ToString(),
                UserName = user.UserName ?? string.Empty,
                Identification = user.Cedula,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Role = "Comercio",
                CommerceId = commerceIdStr,
                CommerceName = commerceName,
                IsActive = user.IsActive
            });
        }

        var totalRecords = list.Count;
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        var data = list
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResultDto<CommerceUserApiDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages > 0 ? totalPages : 1,
            Data = data
        };
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, CreatedUserResponseApiDto? User)> CreateUserApiAsync(CreateUserApiDto dto)
    {
        var normalizedRole = dto.Role.ToLowerInvariant() switch
        {
            "administrador" => "Administrador",
            "cajero" => "Cajero",
            "cliente" => "Cliente",
            _ => dto.Role
        };

        if (normalizedRole == "Comercio" || (normalizedRole != "Administrador" && normalizedRole != "Cajero" && normalizedRole != "Cliente"))
            return (false, "BadRequest", "El rol solo puede ser Administrador, Cajero o Cliente.", null);

        var existingCedula = await _unitOfWork.Users.FindAsync(u => u.Cedula == dto.Identification);
        if (existingCedula.Any())
            return (false, "Conflict", "La cédula ya se encuentra registrada.", null);

        var existingEmail = await _userManager.FindByEmailAsync(dto.Email);
        if (existingEmail != null)
            return (false, "Conflict", "El correo electrónico ya se encuentra registrado.", null);

        var existingUsername = await _userManager.FindByNameAsync(dto.UserName);
        if (existingUsername != null)
            return (false, "Conflict", "El nombre de usuario ya se encuentra registrado.", null);

        if (dto.InitialAmount.HasValue && dto.InitialAmount.Value < 0)
            return (false, "BadRequest", "El monto inicial no puede ser negativo.", null);

        var createUserDto = new CreateUserDto
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Cedula = dto.Identification,
            Email = dto.Email,
            UserName = dto.UserName,
            Password = dto.Password,
            ConfirmPassword = dto.ConfirmPassword,
            Role = normalizedRole,
            InitialBalance = dto.InitialAmount ?? 0
        };

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var result = await _authAppService.RegisterAsync(createUserDto, string.Empty, isApiUser: true);
            if (!result.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return (false, "BadRequest", string.Join(", ", result.Errors.Select(e => e.Description)), null);
            }

            var createdUser = await _userManager.FindByEmailAsync(dto.Email);
            if (createdUser == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return (false, "BadRequest", "Error al recuperar el usuario creado.", null);
            }

            if (normalizedRole == "Cliente")
            {
                string accountNumber = await GenerateUniqueAccountNumberAsync();
                var initialBalance = dto.InitialAmount ?? 0;

                var account = new SavingsAccount
                {
                    ClientId = createdUser.Id,
                    AccountNumber = accountNumber,
                    Balance = initialBalance,
                    AccountType = AccountType.Principal,
                    Status = AccountStatus.Activa,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.SavingsAccounts.AddAsync(account);

                if (initialBalance > 0)
                {
                    var transaction = new Transaction
                    {
                        SavingsAccountId = account.Id,
                        Amount = initialBalance,
                        Type = TransactionType.CRÉDITO,
                        Beneficiary = account.AccountNumber,
                        Origin = "DEPÓSITO",
                        Status = TransactionStatus.APROBADA,
                        Date = DateTime.UtcNow
                    };
                    await _unitOfWork.Transactions.AddAsync(transaction);
                }

                await _unitOfWork.SaveChangesAsync();
            }

            await _unitOfWork.CommitTransactionAsync();

            return (true, null, null, new CreatedUserResponseApiDto
            {
                Id = createdUser.Id.ToString(),
                UserName = createdUser.UserName ?? string.Empty,
                Email = createdUser.Email ?? string.Empty,
                Role = normalizedRole,
                IsActive = false
            });
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, CreatedUserResponseApiDto? User)> CreateCommerceUserApiAsync(Guid commerceId, CreateCommerceUserApiDto dto)
    {
        var merchant = await _unitOfWork.Merchants.GetByIdWithUsersAsync(commerceId);
        if (merchant == null)
            return (false, "NotFound", "El comercio indicado no existe.", null);

        if (merchant.Users.Any())
            return (false, "Conflict", "El comercio ya tiene un usuario asociado.", null);

        var existingCedula = await _unitOfWork.Users.FindAsync(u => u.Cedula == dto.Identification);
        if (existingCedula.Any())
            return (false, "Conflict", "La cédula ya se encuentra registrada.", null);

        var existingEmail = await _userManager.FindByEmailAsync(dto.Email);
        if (existingEmail != null)
            return (false, "Conflict", "El correo electrónico ya se encuentra registrado.", null);

        var existingUsername = await _userManager.FindByNameAsync(dto.UserName);
        if (existingUsername != null)
            return (false, "Conflict", "El nombre de usuario ya se encuentra registrado.", null);

        if (dto.InitialAmount < 0)
            return (false, "BadRequest", "El monto inicial no puede ser negativo.", null);

        var createUserDto = new CreateUserDto
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Cedula = dto.Identification,
            Email = dto.Email,
            UserName = dto.UserName,
            Password = dto.Password,
            ConfirmPassword = dto.ConfirmPassword,
            Role = "Comercio",
            InitialBalance = dto.InitialAmount
        };

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var result = await _authAppService.RegisterAsync(createUserDto, string.Empty, isApiUser: true);
            if (!result.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return (false, "BadRequest", string.Join(", ", result.Errors.Select(e => e.Description)), null);
            }

            var createdUser = await _userManager.FindByEmailAsync(dto.Email);
            if (createdUser == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return (false, "BadRequest", "Error al recuperar el usuario creado.", null);
            }

            createdUser.MerchantId = commerceId;
            await _userManager.UpdateAsync(createdUser);

            string accountNumber = await GenerateUniqueAccountNumberAsync();
            var account = new SavingsAccount
            {
                ClientId = createdUser.Id,
                AccountNumber = accountNumber,
                Balance = dto.InitialAmount,
                AccountType = AccountType.Principal,
                Status = AccountStatus.Activa,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.SavingsAccounts.AddAsync(account);

            if (dto.InitialAmount > 0)
            {
                var transaction = new Transaction
                {
                    SavingsAccountId = account.Id,
                    Amount = dto.InitialAmount,
                    Type = TransactionType.CRÉDITO,
                    Beneficiary = account.AccountNumber,
                    Origin = "DEPÓSITO",
                    Status = TransactionStatus.APROBADA,
                    Date = DateTime.UtcNow
                };
                await _unitOfWork.Transactions.AddAsync(transaction);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return (true, null, null, new CreatedUserResponseApiDto
            {
                Id = createdUser.Id.ToString(),
                UserName = createdUser.UserName ?? string.Empty,
                Email = createdUser.Email ?? string.Empty,
                Role = "Comercio",
                CommerceId = commerceId.ToString(),
                IsActive = false
            });
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> UpdateUserApiAsync(Guid id, UpdateUserApiDto dto)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return (false, "NotFound", "El usuario indicado no existe.");

        var existingCedula = await _unitOfWork.Users.FindAsync(u => u.Cedula == dto.Identification && u.Id != id);
        if (existingCedula.Any())
            return (false, "Conflict", "La cédula ya pertenece a otro usuario.");

        var existingEmail = await _userManager.FindByEmailAsync(dto.Email);
        if (existingEmail != null && existingEmail.Id != id)
            return (false, "Conflict", "El correo electrónico ya pertenece a otro usuario.");

        var existingUsername = await _userManager.FindByNameAsync(dto.UserName);
        if (existingUsername != null && existingUsername.Id != id)
            return (false, "Conflict", "El nombre de usuario ya pertenece a otro usuario.");

        if (dto.AdditionalAmount.HasValue && dto.AdditionalAmount.Value < 0)
            return (false, "BadRequest", "El monto adicional no puede ser negativo.");

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Cedula = dto.Identification;
        user.Email = dto.Email;
        user.UserName = dto.UserName;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(dto.Password))
        {
            if (dto.Password != dto.ConfirmPassword)
                return (false, "BadRequest", "La contraseña y la confirmación de contraseña deben coincidir.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, dto.Password);
            if (!resetResult.Succeeded)
                return (false, "BadRequest", string.Join(", ", resetResult.Errors.Select(e => e.Description)));
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return (false, "BadRequest", "Error al actualizar los datos del usuario.");

        var roles = await _userManager.GetRolesAsync(user);
        var isClientOrCommerce = roles.Contains("Cliente") || roles.Contains("Comercio");

        if (isClientOrCommerce && dto.AdditionalAmount.HasValue && dto.AdditionalAmount.Value > 0)
        {
            var accounts = await _unitOfWork.SavingsAccounts.FindAsync(a => a.ClientId == user.Id && a.AccountType == AccountType.Principal && a.Status == AccountStatus.Activa);
            var mainAccount = accounts.FirstOrDefault();

            if (mainAccount != null)
            {
                mainAccount.Balance += dto.AdditionalAmount.Value;
                _unitOfWork.SavingsAccounts.Update(mainAccount);

                var transaction = new Transaction
                {
                    SavingsAccountId = mainAccount.Id,
                    Amount = dto.AdditionalAmount.Value,
                    Type = TransactionType.CRÉDITO,
                    Beneficiary = mainAccount.AccountNumber,
                    Origin = "DEPÓSITO",
                    Status = TransactionStatus.APROBADA,
                    Date = DateTime.UtcNow
                };
                await _unitOfWork.Transactions.AddAsync(transaction);

                await _unitOfWork.SaveChangesAsync();
            }
        }

        return (true, null, null);
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> UpdateUserStatusApiAsync(Guid id, bool status, Guid adminId)
    {
        if (id == adminId)
            return (false, "Forbidden", "El administrador no puede modificar su propio estado.");

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return (false, "NotFound", "El usuario indicado no existe.");

        user.IsActive = status;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, "BadRequest", "Error al actualizar el estado del usuario.");

        return (true, null, null);
    }

    public async Task<UserDetailApiDto?> GetUserDetailApiAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var mainAccount = (await _unitOfWork.SavingsAccounts.FindAsync(a => a.ClientId == user.Id && a.AccountType == AccountType.Principal)).FirstOrDefault();

        return new UserDetailApiDto
        {
            Id = user.Id.ToString(),
            UserName = user.UserName ?? string.Empty,
            Identification = user.Cedula,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? string.Empty,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            MainAccount = mainAccount != null ? new UserMainAccountApiDto
            {
                AccountNumber = mainAccount.AccountNumber,
                Balance = mainAccount.Balance,
                IsPrincipal = true,
                Status = mainAccount.Status.ToString()
            } : null
        };
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
