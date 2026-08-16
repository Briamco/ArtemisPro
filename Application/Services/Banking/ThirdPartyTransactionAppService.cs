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

public class ThirdPartyTransactionAppService : IThirdPartyTransactionAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<ThirdPartyTransactionAppService> _logger;

    public ThirdPartyTransactionAppService(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<ThirdPartyTransactionAppService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ThirdPartyTransactionPreviewResult> GetPreviewAsync(string sourceAccountNumber, string destinationAccountNumber, decimal amount)
    {
        if (amount <= 0)
        {
            return PreviewFailed("El monto de la transacción debe ser mayor que cero.");
        }

        var source = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(sourceAccountNumber);
        if (source == null || source.Status != AccountStatus.Activa)
        {
            return PreviewFailed("El número de cuenta origen ingresado no corresponde a una cuenta válida.");
        }

        var destination = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(destinationAccountNumber);
        if (destination == null || destination.Status != AccountStatus.Activa)
        {
            return PreviewFailed("El número de cuenta destino ingresado no corresponde a una cuenta válida.");
        }

        if (source.Id == destination.Id)
        {
            return PreviewFailed("La cuenta origen y la cuenta destino no pueden ser la misma.");
        }

        if (source.Balance < amount)
        {
            return PreviewFailed("El monto ingresado excede el saldo disponible de la cuenta.");
        }

        return new ThirdPartyTransactionPreviewResult
        {
            Success = true,
            Preview = new ThirdPartyTransactionPreviewDto
            {
                SourceAccountOwner = BuildOwnerName(source),
                SourceAccountNumber = source.AccountNumber,
                DestinationAccountOwner = BuildOwnerName(destination),
                DestinationAccountNumber = destination.AccountNumber,
                Amount = amount
            }
        };
    }

    public async Task<ThirdPartyTransactionResult> CreateTransactionAsync(Guid tellerId, CreateThirdPartyTransactionDto dto)
    {
        if (dto.Amount <= 0)
        {
            return Failed("El monto de la transacción debe ser mayor que cero.");
        }

        var source = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(dto.SourceAccountNumber);
        if (source == null || source.Status != AccountStatus.Activa)
        {
            return Failed("El número de cuenta origen ingresado no corresponde a una cuenta válida.");
        }

        var destination = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(dto.DestinationAccountNumber);
        if (destination == null || destination.Status != AccountStatus.Activa)
        {
            return await RejectAsync(source, dto.DestinationAccountNumber, dto.Amount, tellerId, "El número de cuenta destino ingresado no corresponde a una cuenta válida.");
        }

        if (source.Id == destination.Id)
        {
            return await RejectAsync(source, destination.AccountNumber, dto.Amount, tellerId, "La cuenta origen y la cuenta destino no pueden ser la misma.");
        }

        if (source.Balance < dto.Amount)
        {
            return await RejectAsync(source, destination.AccountNumber, dto.Amount, tellerId, "El monto ingresado excede el saldo disponible de la cuenta.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            source.Balance -= dto.Amount;
            destination.Balance += dto.Amount;
            _unitOfWork.SavingsAccounts.Update(source);
            _unitOfWork.SavingsAccounts.Update(destination);

            var date = DateTime.UtcNow;

            var debit = new Transaction
            {
                SavingsAccountId = source.Id,
                Amount = dto.Amount,
                Type = TransactionType.DÉBITO,
                Origin = source.AccountNumber,
                Beneficiary = destination.AccountNumber,
                Status = TransactionStatus.APROBADA,
                Date = date,
                PerformedById = tellerId
            };
            await _unitOfWork.Transactions.AddAsync(debit);

            var credit = new Transaction
            {
                SavingsAccountId = destination.Id,
                Amount = dto.Amount,
                Type = TransactionType.CRÉDITO,
                Origin = source.AccountNumber,
                Beneficiary = destination.AccountNumber,
                Status = TransactionStatus.APROBADA,
                Date = date,
                PerformedById = tellerId
            };
            await _unitOfWork.Transactions.AddAsync(credit);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error al procesar la transacción a cuenta de terceros. Origen: {SourceAccountNumber}, Destino: {DestinationAccountNumber}, Monto: {Amount}, Cajero: {TellerId}",
                source.AccountNumber, destination.AccountNumber, dto.Amount, tellerId);
            return Failed("Ocurrió un error al realizar la transacción.");
        }

        var emailSent = await SendNotificationsAsync(source, destination, dto.Amount);

        return new ThirdPartyTransactionResult { Success = true, EmailSent = emailSent };
    }

    private async Task<ThirdPartyTransactionResult> RejectAsync(SavingsAccount source, string beneficiaryAccountNumber, decimal amount, Guid tellerId, string error)
    {
        var rejected = new Transaction
        {
            SavingsAccountId = source.Id,
            Amount = amount,
            Type = TransactionType.DÉBITO,
            Origin = source.AccountNumber,
            Beneficiary = beneficiaryAccountNumber,
            Status = TransactionStatus.RECHAZADA,
            Date = DateTime.UtcNow,
            PerformedById = tellerId
        };

        await _unitOfWork.Transactions.AddAsync(rejected);
        await _unitOfWork.SaveChangesAsync();

        return Failed(error);
    }

    private async Task<bool> SendNotificationsAsync(SavingsAccount source, SavingsAccount destination, decimal amount)
    {
        var originOwnerEmailSent = await SendAsync(
            source.Client?.Email,
            $"Transacción realizada a la cuenta {GetLast4(destination.AccountNumber)}",
            BuildOriginOwnerBody(source, destination, amount));

        var destinationOwnerEmailSent = await SendAsync(
            destination.Client?.Email,
            $"Transacción enviada desde la cuenta {GetLast4(source.AccountNumber)}",
            BuildDestinationOwnerBody(source, destination, amount));

        return originOwnerEmailSent && destinationOwnerEmailSent;
    }

    private string BuildOriginOwnerBody(SavingsAccount source, SavingsAccount destination, decimal amount)
    {
        return $"Hola {BuildOwnerName(source)},<br><br>" +
               "Se ha realizado una transacción desde su cuenta de ahorro hacia otra cuenta.<br><br>" +
               $"Monto transferido: RD${amount:N2}<br>" +
               $"Cuenta origen terminada en: {GetLast4(source.AccountNumber)}<br>" +
               $"Cuenta destino terminada en: {GetLast4(destination.AccountNumber)}<br>" +
               $"Fecha y hora: {DateTime.UtcNow:dd/MM/yyyy hh:mm:ss tt}<br><br>" +
               "Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";
    }

    private string BuildDestinationOwnerBody(SavingsAccount source, SavingsAccount destination, decimal amount)
    {
        return $"Hola {BuildOwnerName(destination)},<br><br>" +
               "Se ha recibido una transacción en su cuenta de ahorro.<br><br>" +
               $"Monto recibido: RD${amount:N2}<br>" +
               $"Cuenta origen terminada en: {GetLast4(source.AccountNumber)}<br>" +
               $"Cuenta destino terminada en: {GetLast4(destination.AccountNumber)}<br>" +
               $"Fecha y hora: {DateTime.UtcNow:dd/MM/yyyy hh:mm:ss tt}<br><br>" +
               "Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";
    }

    private async Task<bool> SendAsync(string? to, string subject, string body)
    {
        if (string.IsNullOrEmpty(to))
        {
            return true;
        }

        try
        {
            await _emailService.SendAsync(to, subject, body);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private string BuildOwnerName(SavingsAccount account)
    {
        if (account.Client == null) return string.Empty;
        return $"{account.Client.FirstName} {account.Client.LastName}".Trim();
    }

    private string GetLast4(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length >= 4 ? value.Substring(value.Length - 4) : value;
    }

    private ThirdPartyTransactionResult Failed(string error)
    {
        return new ThirdPartyTransactionResult { Success = false, Error = error, EmailSent = false };
    }

    private ThirdPartyTransactionPreviewResult PreviewFailed(string error)
    {
        return new ThirdPartyTransactionPreviewResult { Success = false, Error = error };
    }
}
