using System;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Shared.Interfaces;

namespace Application.Services.Banking;

public class WithdrawalAppService : IWithdrawalAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<WithdrawalAppService> _logger;

    public WithdrawalAppService(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<WithdrawalAppService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<WithdrawalPreviewDto?> GetWithdrawalPreviewAsync(string accountNumber)
    {
        var account = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(accountNumber);
        if (account == null || account.Status != AccountStatus.Activa)
        {
            return null;
        }

        return new WithdrawalPreviewDto
        {
            AccountNumber = account.AccountNumber,
            ClientName = $"{account.Client.FirstName} {account.Client.LastName}"
        };
    }

    public async Task<WithdrawalResult> CreateWithdrawalAsync(Guid tellerId, CreateWithdrawalDto dto)
    {
        var account = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(dto.AccountNumber);
        if (account == null || account.Status != AccountStatus.Activa)
        {
            return Failed("El número de cuenta ingresado no corresponde a una cuenta válida.");
        }

        if (dto.Amount <= 0)
        {
            return Failed("El monto a retirar debe ser mayor que cero.");
        }

        if (account.Balance < dto.Amount)
        {
            return await RejectAsync(account, dto.Amount, tellerId, "El monto ingresado excede el saldo disponible de la cuenta.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            account.Balance -= dto.Amount;
            _unitOfWork.SavingsAccounts.Update(account);

            var transaction = new Transaction
            {
                SavingsAccountId = account.Id,
                Amount = dto.Amount,
                Type = TransactionType.DÉBITO,
                Origin = account.AccountNumber,
                Beneficiary = "RETIRO",
                Status = TransactionStatus.APROBADA,
                Date = DateTime.UtcNow,
                PerformedById = tellerId
            };
            await _unitOfWork.Transactions.AddAsync(transaction);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error al procesar el retiro. Cuenta origen: {AccountNumber}, Monto: {Amount}, Cajero: {TellerId}",
                account.AccountNumber, dto.Amount, tellerId);
            return Failed("Ocurrió un error al realizar el retiro.");
        }

        var emailSent = await SendWithdrawalNotificationAsync(account, dto.Amount);

        return new WithdrawalResult { Success = true, EmailSent = emailSent };
    }

    private async Task<WithdrawalResult> RejectAsync(SavingsAccount account, decimal amount, Guid tellerId, string error)
    {
        var rejected = new Transaction
        {
            SavingsAccountId = account.Id,
            Amount = amount,
            Type = TransactionType.DÉBITO,
            Origin = account.AccountNumber,
            Beneficiary = "RETIRO",
            Status = TransactionStatus.RECHAZADA,
            Date = DateTime.UtcNow,
            PerformedById = tellerId
        };

        await _unitOfWork.Transactions.AddAsync(rejected);
        await _unitOfWork.SaveChangesAsync();

        return Failed(error);
    }

    private async Task<bool> SendWithdrawalNotificationAsync(SavingsAccount account, decimal amount)
    {
        try
        {
            if (account.Client == null || string.IsNullOrEmpty(account.Client.Email))
            {
                return false;
            }

            var last4 = GetLast4(account.AccountNumber);
            var subject = $"Retiro realizado desde su cuenta {last4}";
            var body = $"Hola {account.Client.FirstName} {account.Client.LastName},<br><br>" +
                       $"Se ha realizado un retiro desde su cuenta terminada en {last4}.<br><br>" +
                       $"Monto retirado: RD${amount:N2}<br>" +
                       $"Fecha y hora: {DateTime.UtcNow:dd/MM/yyyy hh:mm:ss tt}<br><br>" +
                       "Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";

            await _emailService.SendAsync(account.Client.Email, subject, body);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private string GetLast4(string value)
    {
        return value.Length >= 4 ? value.Substring(value.Length - 4) : value;
    }

    private WithdrawalResult Failed(string error)
    {
        return new WithdrawalResult { Success = false, Error = error, EmailSent = false };
    }
}
