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

public class CardPaymentAppService : ICardPaymentAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<CardPaymentAppService> _logger;

    public CardPaymentAppService(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<CardPaymentAppService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<CardPaymentPreviewDto?> GetCardPaymentPreviewAsync(string accountNumber, string cardNumber, decimal amount)
    {
        if (amount <= 0 || string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length != 16)
        {
            return null;
        }

        var account = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(accountNumber);
        if (account == null || account.Status != AccountStatus.Activa)
        {
            return null;
        }

        var card = await _unitOfWork.CreditCards.GetByCardNumberAsync(cardNumber);
        if (card == null || card.Status != CardStatus.Activa || card.Debt <= 0)
        {
            return null;
        }

        return new CardPaymentPreviewDto
        {
            OriginAccountNumber = account.AccountNumber,
            OriginAccountClientName = $"{account.Client.FirstName} {account.Client.LastName}",
            CardLast4 = GetLast4(card.CardNumber),
            CardClientName = $"{card.Client.FirstName} {card.Client.LastName}",
            EnteredAmount = amount,
            EffectiveAmount = Math.Min(amount, card.Debt)
        };
    }

    public async Task<CardPaymentResult> CreateCardPaymentAsync(Guid tellerId, CreateCardPaymentDto dto)
    {
        if (dto.Amount <= 0)
        {
            return Failed("El monto a pagar debe ser mayor que cero.");
        }

        if (string.IsNullOrWhiteSpace(dto.CardNumber) || dto.CardNumber.Length != 16)
        {
            return Failed("El número de tarjeta debe contener 16 dígitos.");
        }

        var account = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(dto.AccountNumber);
        if (account == null || account.Status != AccountStatus.Activa)
        {
            return Failed("El número de cuenta ingresado no corresponde a una cuenta válida.");
        }

        var card = await _unitOfWork.CreditCards.GetByCardNumberAsync(dto.CardNumber);
        if (card == null || card.Status != CardStatus.Activa)
        {
            return await RejectAsync(account, GetLast4(dto.CardNumber), dto.Amount, tellerId, "El número de tarjeta ingresado no corresponde a una tarjeta válida.");
        }

        if (card.Debt <= 0)
        {
            return await RejectAsync(account, GetLast4(card.CardNumber), dto.Amount, tellerId, "La tarjeta seleccionada no tiene deuda pendiente.");
        }

        var effectiveAmount = Math.Min(dto.Amount, card.Debt);

        if (account.Balance < effectiveAmount)
        {
            return await RejectAsync(account, GetLast4(card.CardNumber), effectiveAmount, tellerId, "El monto ingresado excede el saldo disponible de la cuenta.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            account.Balance -= effectiveAmount;
            card.Debt -= effectiveAmount;
            _unitOfWork.SavingsAccounts.Update(account);
            _unitOfWork.CreditCards.Update(card);

            var transaction = new Transaction
            {
                SavingsAccountId = account.Id,
                Amount = effectiveAmount,
                Type = TransactionType.DÉBITO,
                Origin = account.AccountNumber,
                Beneficiary = GetLast4(card.CardNumber),
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
            _logger.LogError(ex, "Error al procesar el pago a tarjeta. Cuenta origen: {AccountNumber}, Tarjeta: {CardNumber}, Monto: {Amount}, Cajero: {TellerId}",
                account.AccountNumber, dto.CardNumber, effectiveAmount, tellerId);
            return Failed("Ocurrió un error al realizar el pago.");
        }

        var emailSent = await SendPaymentNotificationAsync(account, card, effectiveAmount);

        return new CardPaymentResult { Success = true, EmailSent = emailSent };
    }

    private async Task<CardPaymentResult> RejectAsync(SavingsAccount account, string cardLast4, decimal amount, Guid tellerId, string error)
    {
        var rejected = new Transaction
        {
            SavingsAccountId = account.Id,
            Amount = amount,
            Type = TransactionType.DÉBITO,
            Origin = account.AccountNumber,
            Beneficiary = cardLast4,
            Status = TransactionStatus.RECHAZADA,
            Date = DateTime.UtcNow,
            PerformedById = tellerId
        };

        await _unitOfWork.Transactions.AddAsync(rejected);
        await _unitOfWork.SaveChangesAsync();

        return Failed(error);
    }

    private async Task<bool> SendPaymentNotificationAsync(SavingsAccount account, CreditCard card, decimal amount)
    {
        var cardOwnerEmailSent = true;
        var accountOwnerEmailSent = true;

        if (card.Client != null && !string.IsNullOrEmpty(card.Client.Email))
        {
            cardOwnerEmailSent = await SendAsync(
                card.Client.Email,
                $"Pago realizado a la tarjeta {GetLast4(card.CardNumber)}",
                BuildCardOwnerBody(card, account, amount));
        }

        if (account.ClientId != card.ClientId && account.Client != null && !string.IsNullOrEmpty(account.Client.Email))
        {
            accountOwnerEmailSent = await SendAsync(
                account.Client.Email,
                $"Débito realizado desde su cuenta {GetLast4(account.AccountNumber)}",
                BuildAccountOwnerBody(account, card, amount));
        }

        return cardOwnerEmailSent && accountOwnerEmailSent;
    }

    private string BuildCardOwnerBody(CreditCard card, SavingsAccount account, decimal amount)
    {
        return $"Hola {card.Client.FirstName} {card.Client.LastName},<br><br>" +
               $"Se ha realizado un pago a su tarjeta de crédito terminada en {GetLast4(card.CardNumber)}.<br><br>" +
               $"Monto pagado: RD${amount:N2}<br>" +
               $"Cuenta origen terminada en: {GetLast4(account.AccountNumber)}<br>" +
               $"Fecha y hora: {DateTime.UtcNow:dd/MM/yyyy hh:mm:ss tt}<br><br>" +
               "Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";
    }

    private string BuildAccountOwnerBody(SavingsAccount account, CreditCard card, decimal amount)
    {
        return $"Hola {account.Client.FirstName} {account.Client.LastName},<br><br>" +
               $"Se ha realizado un débito de RD${amount:N2} desde su cuenta terminada en {GetLast4(account.AccountNumber)} " +
               $"para realizar un pago a la tarjeta de crédito terminada en {GetLast4(card.CardNumber)}.<br><br>" +
               $"Fecha y hora: {DateTime.UtcNow:dd/MM/yyyy hh:mm:ss tt}<br><br>" +
               "Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";
    }

    private async Task<bool> SendAsync(string to, string subject, string body)
    {
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

    private string GetLast4(string value)
    {
        return value.Length >= 4 ? value.Substring(value.Length - 4) : value;
    }

    private CardPaymentResult Failed(string error)
    {
        return new CardPaymentResult { Success = false, Error = error, EmailSent = false };
    }
}
