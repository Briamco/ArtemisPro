using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Banking;

public class SavingsAccountAppService : ISavingsAccountAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SavingsAccountAppService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SavingsAccountDto>> GetSavingsAccountsAsync(string? status = null, string? type = null, string? cedula = null)
    {
        var accounts = await _unitOfWork.SavingsAccounts.GetAllAsync();
        
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AccountStatus>(status, out var parsedStatus))
        {
            accounts = accounts.Where(a => a.Status == parsedStatus);
        }
        
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<AccountType>(type, out var parsedType))
        {
            accounts = accounts.Where(a => a.AccountType == parsedType);
        }
        
        if (!string.IsNullOrEmpty(cedula))
        {
            accounts = accounts.Where(a => a.Client != null && a.Client.Cedula.Contains(cedula));
        }

        return _mapper.Map<IEnumerable<SavingsAccountDto>>(accounts);
    }

    public async Task<SavingsAccountDto?> GetSavingsAccountByIdAsync(Guid id)
    {
        var account = await _unitOfWork.SavingsAccounts.GetByIdAsync(id);
        if (account == null) return null;
        
        return _mapper.Map<SavingsAccountDto>(account);
    }

    public async Task<IEnumerable<TransactionDto>> GetTransactionsAsync(Guid accountId)
    {
        var transactions = await _unitOfWork.Transactions.GetBySavingsAccountIdAsync(accountId);
        return _mapper.Map<IEnumerable<TransactionDto>>(transactions);
    }

    public async Task<(bool Success, string? Error)> CreateSavingsAccountAsync(CreateSavingsAccountDto dto)
    {
        var user = (await _unitOfWork.Users.FindAsync(u => u.Id == dto.ClientId)).FirstOrDefault();
        if (user == null)
        {
            return (false, "El cliente no existe.");
        }
        if (!user.IsActive)
        {
            return (false, "El cliente no está activo.");
        }

        if (!user.IsActive)
        {
            return (false, "El cliente está inactivo.");
        }

        var primaryAccount = await _unitOfWork.SavingsAccounts.GetPrimaryByClientIdAsync(dto.ClientId);
        
        if (primaryAccount == null || primaryAccount.Status != AccountStatus.Activa)
        {
            return (false, "El cliente debe tener una cuenta de ahorro principal activa para poder asignarle una cuenta secundaria.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            string accountNumber = await GenerateUniqueAccountNumberAsync();
            
            var newAccount = new SavingsAccount
            {
                ClientId = dto.ClientId,
                AccountNumber = accountNumber,
                Balance = dto.InitialBalance,
                AccountType = AccountType.Secundaria,
                Status = AccountStatus.Activa,
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.SavingsAccounts.AddAsync(newAccount);

            if (dto.InitialBalance > 0)
            {
                var transaction = new Transaction
                {
                    SavingsAccountId = newAccount.Id,
                    Amount = dto.InitialBalance,
                    Type = TransactionType.CRÉDITO,
                    Beneficiary = newAccount.AccountNumber,
                    Origin = "DEPÓSITO",
                    Status = TransactionStatus.APROBADA,
                    Date = DateTime.UtcNow
                };
                await _unitOfWork.Transactions.AddAsync(transaction);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            
            return (true, null);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return (false, "Ocurrió un error al crear la cuenta de ahorro.");
        }
    }

    public async Task<(bool Success, string? Error)> CancelSavingsAccountAsync(Guid id)
    {
        var account = await _unitOfWork.SavingsAccounts.GetByIdAsync(id);
        if (account == null) return (false, "La cuenta seleccionada no existe.");

        if (account.Status == AccountStatus.Cancelada)
        {
            return (false, "La cuenta seleccionada ya se encuentra cancelada.");
        }
        
        if (account.AccountType == AccountType.Principal)
        {
            return (false, "Las cuentas principales no pueden ser canceladas.");
        }

        var primaryAccount = await _unitOfWork.SavingsAccounts.GetPrimaryByClientIdAsync(account.ClientId);
        if (primaryAccount == null || primaryAccount.Status != AccountStatus.Activa)
        {
            return (false, "No es posible cancelar la cuenta porque el cliente no tiene una cuenta principal activa para recibir los fondos.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            decimal transferAmount = account.Balance;
            
            if (transferAmount > 0)
            {
                var debitTransaction = new Transaction
                {
                    SavingsAccountId = account.Id,
                    Amount = transferAmount,
                    Type = TransactionType.DÉBITO,
                    Origin = account.AccountNumber,
                    Beneficiary = primaryAccount.AccountNumber,
                    Status = TransactionStatus.APROBADA,
                    Date = DateTime.UtcNow
                };
                await _unitOfWork.Transactions.AddAsync(debitTransaction);
                
                var creditTransaction = new Transaction
                {
                    SavingsAccountId = primaryAccount.Id,
                    Amount = transferAmount,
                    Type = TransactionType.CRÉDITO,
                    Origin = account.AccountNumber,
                    Beneficiary = primaryAccount.AccountNumber,
                    Status = TransactionStatus.APROBADA,
                    Date = DateTime.UtcNow
                };
                await _unitOfWork.Transactions.AddAsync(creditTransaction);

                account.Balance = 0;
                primaryAccount.Balance += transferAmount;
                _unitOfWork.SavingsAccounts.Update(primaryAccount);
            }

            account.Status = AccountStatus.Cancelada;
            _unitOfWork.SavingsAccounts.Update(account);
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            
            return (true, null);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return (false, "Ocurrió un error al transferir los fondos y cancelar la cuenta.");
        }
    }
    
    private async Task<string> GenerateUniqueAccountNumberAsync()
    {
        string accountNumber;
        bool existsInSavings;
        bool existsInLoans;
        
        do
        {
            accountNumber = RandomNumberGenerator.GetInt32(100000000, 1000000000).ToString();
            
            existsInSavings = await _unitOfWork.SavingsAccounts.ExistsAsync(a => a.AccountNumber == accountNumber);
            existsInLoans = await _unitOfWork.Loans.ExistsAsync(l => l.LoanNumber == accountNumber);
            
        } while (existsInSavings || existsInLoans);

        return accountNumber;
    }

    public async Task<IEnumerable<SavingsAccountDto>> GetClientAccountsAsync(Guid clientId)
    {
        var accounts = await _unitOfWork.SavingsAccounts.GetByClientIdAsync(clientId);
        return _mapper.Map<IEnumerable<SavingsAccountDto>>(accounts);
    }

    public async Task<PagedResultDto<SavingsAccountApiDto>> GetSavingsAccountsPagedApiAsync(
        int page, int pageSize, string? status, string? type, string? identification)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 20) pageSize = 20;

        var allAccounts = await _unitOfWork.SavingsAccounts.GetAllAsync();
        var query = allAccounts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(identification))
        {
            query = query.Where(a => a.Client != null && a.Client.Cedula.Contains(identification));
        }

        if (!string.IsNullOrWhiteSpace(type) && !type.Equals("todas", StringComparison.OrdinalIgnoreCase))
        {
            if (type.Equals("principal", StringComparison.OrdinalIgnoreCase))
                query = query.Where(a => a.AccountType == AccountType.Principal);
            else if (type.Equals("secundaria", StringComparison.OrdinalIgnoreCase))
                query = query.Where(a => a.AccountType == AccountType.Secundaria);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("activa", StringComparison.OrdinalIgnoreCase))
                query = query.Where(a => a.Status == AccountStatus.Activa);
            else if (status.Equals("cancelada", StringComparison.OrdinalIgnoreCase))
                query = query.Where(a => a.Status == AccountStatus.Cancelada);
            // "todas" leaves it unfiltered
        }
        else if (string.IsNullOrWhiteSpace(identification))
        {
            // By default, show active accounts
            query = query.Where(a => a.Status == AccountStatus.Activa);
        }

        IOrderedQueryable<SavingsAccount> orderedQuery;
        if (!string.IsNullOrWhiteSpace(identification) && string.IsNullOrWhiteSpace(status))
        {
            orderedQuery = query
                .OrderBy(a => a.Status == AccountStatus.Activa ? 0 : 1)
                .ThenByDescending(a => a.CreatedAt);
        }
        else
        {
            orderedQuery = query.OrderByDescending(a => a.CreatedAt);
        }

        var totalRecords = orderedQuery.Count();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        var items = orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new SavingsAccountApiDto
            {
                Id = a.Id.ToString(),
                AccountNumber = a.AccountNumber,
                ClientId = a.ClientId.ToString(),
                ClientFullName = a.Client != null ? $"{a.Client.FirstName} {a.Client.LastName}".Trim() : string.Empty,
                Identification = a.Client != null ? a.Client.Cedula : string.Empty,
                Balance = a.Balance,
                Type = a.AccountType.ToString(),
                Status = a.Status.ToString(),
                CreatedAt = a.CreatedAt
            })
            .ToList();

        return new PagedResultDto<SavingsAccountApiDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages > 0 ? totalPages : 1,
            Data = items
        };
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, SavingsAccountApiDto? Account)> CreateSavingsAccountApiAsync(
        CreateSavingsAccountApiDto dto, Guid? adminId)
    {
        if (!Guid.TryParse(dto.ClientId, out var clientGuid))
            return (false, "NotFound", "El cliente indicado no existe.", null);

        var user = (await _unitOfWork.Users.FindAsync(u => u.Id == clientGuid)).FirstOrDefault();
        if (user == null)
            return (false, "NotFound", "El cliente indicado no existe.", null);

        if (!user.IsActive)
            return (false, "BadRequest", "El cliente no se encuentra activo.", null);

        if (dto.InitialBalance < 0)
            return (false, "BadRequest", "El balance inicial no puede ser negativo.", null);

        var primaryAccount = await _unitOfWork.SavingsAccounts.GetPrimaryByClientIdAsync(clientGuid);
        if (primaryAccount == null || primaryAccount.Status != AccountStatus.Activa)
            return (false, "BadRequest", "El cliente debe tener una cuenta principal activa antes de crear una secundaria.", null);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            string accountNumber = await GenerateUniqueAccountNumberAsync();

            var newAccount = new SavingsAccount
            {
                ClientId = clientGuid,
                AccountNumber = accountNumber,
                Balance = dto.InitialBalance,
                AccountType = AccountType.Secundaria,
                Status = AccountStatus.Activa,
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.SavingsAccounts.AddAsync(newAccount);

            if (dto.InitialBalance > 0)
            {
                var transaction = new Transaction
                {
                    SavingsAccountId = newAccount.Id,
                    Amount = dto.InitialBalance,
                    Type = TransactionType.CRÉDITO,
                    Beneficiary = newAccount.AccountNumber,
                    Origin = "DEPÓSITO",
                    Status = TransactionStatus.APROBADA,
                    Date = DateTime.UtcNow,
                    PerformedById = adminId
                };
                await _unitOfWork.Transactions.AddAsync(transaction);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var responseDto = new SavingsAccountApiDto
            {
                Id = newAccount.Id.ToString(),
                AccountNumber = newAccount.AccountNumber,
                ClientId = newAccount.ClientId.ToString(),
                ClientFullName = $"{user.FirstName} {user.LastName}".Trim(),
                Identification = user.Cedula,
                Balance = newAccount.Balance,
                Type = "Secundaria",
                Status = "Activa",
                CreatedAt = newAccount.CreatedAt
            };

            return (true, null, null, responseDto);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<SavingsAccountDetailWithTransactionsApiDto?> GetAccountTransactionsApiAsync(
        string accountNumber, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 20) pageSize = 20;

        var account = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(accountNumber);
        if (account == null)
            return null;

        var user = (await _unitOfWork.Users.FindAsync(u => u.Id == account.ClientId)).FirstOrDefault();
        var clientFullName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : string.Empty;

        var allTransactions = await _unitOfWork.Transactions.GetBySavingsAccountIdAsync(account.Id);
        var orderedTransactions = allTransactions
            .OrderByDescending(t => t.Date)
            .ToList();

        var totalRecords = orderedTransactions.Count;
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var pagedItems = orderedTransactions
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new SavingsAccountTransactionItemApiDto
            {
                Id = t.Id.ToString(),
                Date = t.Date,
                Amount = t.Amount,
                TransactionType = t.Type.ToString(),
                Origin = t.Origin,
                Beneficiary = t.Beneficiary,
                Status = t.Status.ToString()
            })
            .ToList();

        return new SavingsAccountDetailWithTransactionsApiDto
        {
            AccountNumber = account.AccountNumber,
            ClientFullName = clientFullName,
            Balance = account.Balance,
            Type = account.AccountType.ToString(),
            Status = account.Status.ToString(),
            Transactions = new PagedResultDto<SavingsAccountTransactionItemApiDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages > 0 ? totalPages : 1,
                Data = pagedItems
            }
        };
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> CancelSavingsAccountApiAsync(string accountNumber)
    {
        var account = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(accountNumber);
        if (account == null)
            return (false, "NotFound", "La cuenta indicada no existe.");

        if (account.Status == AccountStatus.Cancelada)
            return (false, "BadRequest", "La cuenta ya se encuentra cancelada.");

        if (account.AccountType == AccountType.Principal)
            return (false, "BadRequest", "Las cuentas principales no pueden ser canceladas.");

        var primaryAccount = await _unitOfWork.SavingsAccounts.GetPrimaryByClientIdAsync(account.ClientId);
        if (primaryAccount == null || primaryAccount.Status != AccountStatus.Activa)
            return (false, "BadRequest", "El cliente debe tener una cuenta principal activa para recibir los fondos.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            decimal transferAmount = account.Balance;

            if (transferAmount > 0)
            {
                var debitTransaction = new Transaction
                {
                    SavingsAccountId = account.Id,
                    Amount = transferAmount,
                    Type = TransactionType.DÉBITO,
                    Origin = account.AccountNumber,
                    Beneficiary = primaryAccount.AccountNumber,
                    Status = TransactionStatus.APROBADA,
                    Date = DateTime.UtcNow
                };
                await _unitOfWork.Transactions.AddAsync(debitTransaction);

                var creditTransaction = new Transaction
                {
                    SavingsAccountId = primaryAccount.Id,
                    Amount = transferAmount,
                    Type = TransactionType.CRÉDITO,
                    Origin = account.AccountNumber,
                    Beneficiary = primaryAccount.AccountNumber,
                    Status = TransactionStatus.APROBADA,
                    Date = DateTime.UtcNow
                };
                await _unitOfWork.Transactions.AddAsync(creditTransaction);

                account.Balance = 0;
                primaryAccount.Balance += transferAmount;
                _unitOfWork.SavingsAccounts.Update(primaryAccount);
            }

            account.Status = AccountStatus.Cancelada;
            _unitOfWork.SavingsAccounts.Update(account);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return (true, null, null);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
