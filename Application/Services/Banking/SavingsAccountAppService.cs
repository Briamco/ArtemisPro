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
                    Beneficiary = $"{user.FirstName} {user.LastName}",
                    Origin = "Apertura de cuenta",
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
                    Beneficiary = primaryAccount.AccountNumber,
                    Origin = "Cancelación de cuenta",
                    Status = TransactionStatus.APROBADA,
                    Date = DateTime.UtcNow
                };
                await _unitOfWork.Transactions.AddAsync(debitTransaction);
                
                var creditTransaction = new Transaction
                {
                    SavingsAccountId = primaryAccount.Id,
                    Amount = transferAmount,
                    Type = TransactionType.CRÉDITO,
                    Beneficiary = "Transferencia de cuenta cancelada",
                    Origin = account.AccountNumber,
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
}
