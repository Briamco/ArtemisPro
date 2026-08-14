using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Shared.Interfaces;

namespace Application.Services.Banking;

public class TransferAppService : ITransferAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly ILogger<TransferAppService> _logger;

    public TransferAppService(IUnitOfWork unitOfWork, IMapper mapper, IEmailService emailService, ILogger<TransferAppService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IEnumerable<SavingsAccountDto>> GetActiveSavingsAccountsByClientIdAsync(Guid clientId)
    {
        var accounts = await _unitOfWork.SavingsAccounts.GetByClientIdAsync(clientId);
        return _mapper.Map<IEnumerable<SavingsAccountDto>>(accounts.Where(a => a.Status == AccountStatus.Activa));
    }

    public async Task<TransferResult> CreateTransferAsync(Guid clientId, CreateTransferDto dto)
    {
        var origin = await _unitOfWork.SavingsAccounts.GetByIdAsync(dto.OriginAccountId);
        if (origin == null)
        {
            return Failed("La cuenta de origen seleccionada no existe.");
        }

        if (origin.ClientId != clientId)
        {
            return Failed("La cuenta de origen debe pertenecer al cliente autenticado.");
        }

        if (origin.Status != AccountStatus.Activa)
        {
            return Failed("La cuenta de origen debe estar activa.");
        }

        var destination = await _unitOfWork.SavingsAccounts.GetByIdAsync(dto.DestinationAccountId);
        if (destination == null)
        {
            return await RejectAsync(origin, string.Empty, dto.Amount, "La cuenta de destino seleccionada no existe.");
        }

        if (destination.ClientId != clientId)
        {
            return await RejectAsync(origin, destination.AccountNumber, dto.Amount, "La cuenta de destino debe pertenecer al cliente autenticado.");
        }

        if (destination.Status != AccountStatus.Activa)
        {
            return await RejectAsync(origin, destination.AccountNumber, dto.Amount, "La cuenta de destino debe estar activa.");
        }

        if (dto.Amount <= 0)
        {
            return await RejectAsync(origin, destination.AccountNumber, dto.Amount, "El monto a transferir debe ser mayor que cero.");
        }

        if (origin.Id == destination.Id)
        {
            return await RejectAsync(origin, destination.AccountNumber, dto.Amount, "La cuenta de origen y la cuenta de destino no pueden ser la misma.");
        }

        var activeAccounts = await _unitOfWork.SavingsAccounts.GetByClientIdAsync(clientId);
        if (activeAccounts.Count(a => a.Status == AccountStatus.Activa) < 2)
        {
            return await RejectAsync(origin, destination.AccountNumber, dto.Amount, "Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas.");
        }

        if (origin.Balance < dto.Amount)
        {
            return await RejectAsync(origin, destination.AccountNumber, dto.Amount, "No dispone del monto requerido en la cuenta seleccionada.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            origin.Balance -= dto.Amount;
            destination.Balance += dto.Amount;
            _unitOfWork.SavingsAccounts.Update(origin);
            _unitOfWork.SavingsAccounts.Update(destination);

            var debitTransaction = new Transaction
            {
                SavingsAccountId = origin.Id,
                Amount = dto.Amount,
                Type = TransactionType.Debito,
                Beneficiary = destination.AccountNumber,
                Origin = origin.AccountNumber,
                Status = TransactionStatus.Aprobada,
                Date = DateTime.UtcNow
            };
            await _unitOfWork.Transactions.AddAsync(debitTransaction);

            var creditTransaction = new Transaction
            {
                SavingsAccountId = destination.Id,
                Amount = dto.Amount,
                Type = TransactionType.Credito,
                Beneficiary = destination.AccountNumber,
                Origin = origin.AccountNumber,
                Status = TransactionStatus.Aprobada,
                Date = DateTime.UtcNow
            };
            await _unitOfWork.Transactions.AddAsync(creditTransaction);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error al procesar la transferencia entre cuentas del cliente {ClientId}. Origen: {OriginAccount}, Destino: {DestinationAccount}, Monto: {Amount}",
                clientId, origin.AccountNumber, destination.AccountNumber, dto.Amount);
            return Failed("Ocurrió un error al realizar la transferencia.");
        }

        var emailSent = await SendTransferNotificationAsync(clientId, origin, destination, dto.Amount);

        return new TransferResult { Success = true, EmailSent = emailSent };
    }

    private async Task<TransferResult> RejectAsync(SavingsAccount origin, string beneficiaryAccountNumber, decimal amount, string error)
    {
        await RecordRejectedAttemptAsync(origin, beneficiaryAccountNumber, amount);
        return Failed(error);
    }

    private async Task RecordRejectedAttemptAsync(SavingsAccount origin, string beneficiaryAccountNumber, decimal amount)
    {
        var rejected = new Transaction
        {
            SavingsAccountId = origin.Id,
            Amount = amount,
            Type = TransactionType.Debito,
            Beneficiary = beneficiaryAccountNumber,
            Origin = origin.AccountNumber,
            Status = TransactionStatus.Rechazada,
            Date = DateTime.UtcNow
        };

        await _unitOfWork.Transactions.AddAsync(rejected);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<bool> SendTransferNotificationAsync(Guid clientId, SavingsAccount origin, SavingsAccount destination, decimal amount)
    {
        try
        {
            var client = await _unitOfWork.Users.GetByIdAsync(clientId);
            if (client == null || string.IsNullOrEmpty(client.Email))
            {
                return false;
            }

            var subject = "Transferencia entre cuentas realizada";
            var last4Origin = origin.AccountNumber.Substring(origin.AccountNumber.Length - 4);
            var last4Destination = destination.AccountNumber.Substring(destination.AccountNumber.Length - 4);
            var body = $"Hola {client.FirstName} {client.LastName},<br><br>" +
                       "Se ha realizado una transferencia entre sus cuentas de ahorro.<br><br>" +
                       $"Cuenta origen terminada en: {last4Origin}<br>" +
                       $"Cuenta destino terminada en: {last4Destination}<br>" +
                       $"Monto transferido: RD${amount:N2}<br>" +
                       $"Fecha y hora: {DateTime.UtcNow:dd/MM/yyyy hh:mm:ss tt}<br><br>" +
                       "Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";

            await _emailService.SendAsync(client.Email, subject, body);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private TransferResult Failed(string error)
    {
        return new TransferResult { Success = false, Error = error, EmailSent = false };
    }
}
