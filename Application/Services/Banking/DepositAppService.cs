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

public class DepositAppService : IDepositAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<DepositAppService> _logger;

    public DepositAppService(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<DepositAppService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<DepositPreviewDto?> GetDepositPreviewAsync(string accountNumber)
    {
        var account = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(accountNumber);
        if (account == null || account.Status != AccountStatus.Activa)
        {
            return null;
        }

        return new DepositPreviewDto
        {
            AccountNumber = account.AccountNumber,
            ClientName = $"{account.Client.FirstName} {account.Client.LastName}"
        };
    }

    public async Task<DepositResult> CreateDepositAsync(Guid tellerId, CreateDepositDto dto)
    {
        if (dto.Amount <= 0)
        {
            return Failed("El monto a depositar debe ser mayor que cero.");
        }

        var account = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(dto.AccountNumber);
        if (account == null || account.Status != AccountStatus.Activa)
        {
            return Failed("El número de cuenta ingresado no corresponde a una cuenta válida.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            account.Balance += dto.Amount;
            _unitOfWork.SavingsAccounts.Update(account);

            var transaction = new Transaction
            {
                SavingsAccountId = account.Id,
                Amount = dto.Amount,
                Type = TransactionType.CRÉDITO,
                Origin = "DEPÓSITO",
                Beneficiary = account.AccountNumber,
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
            _logger.LogError(ex, "Error al procesar el depósito. Cuenta destino: {AccountNumber}, Monto: {Amount}, Cajero: {TellerId}",
                account.AccountNumber, dto.Amount, tellerId);
            return Failed("Ocurrió un error al realizar el depósito.");
        }

        var emailSent = await SendDepositNotificationAsync(account, dto.Amount);

        return new DepositResult { Success = true, EmailSent = emailSent };
    }

    private async Task<bool> SendDepositNotificationAsync(SavingsAccount account, decimal amount)
    {
        try
        {
            if (account.Client == null || string.IsNullOrEmpty(account.Client.Email))
            {
                return false;
            }

            var last4 = account.AccountNumber.Substring(account.AccountNumber.Length - 4);
            var subject = $"Depósito realizado a su cuenta {last4}";
            var body = $"Hola {account.Client.FirstName} {account.Client.LastName},<br><br>" +
                       $"Se ha realizado un depósito a su cuenta terminada en {last4}.<br><br>" +
                       $"Monto depositado: RD${amount:N2}<br>" +
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

    private DepositResult Failed(string error)
    {
        return new DepositResult { Success = false, Error = error, EmailSent = false };
    }
}
