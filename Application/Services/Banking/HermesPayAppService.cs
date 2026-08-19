using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Shared.Interfaces;

namespace Application.Services.Banking;

public class HermesPayAppService : IHermesPayAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;

    public HermesPayAppService(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _emailService = emailService;
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, CommerceTransactionsResponseDto? Result)> GetCommerceTransactionsAsync(
        Guid commerceId, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 20) pageSize = 20;

        var merchant = await _unitOfWork.Merchants.GetByIdWithUsersAsync(commerceId);
        if (merchant == null)
            return (false, "NotFound", "El comercio indicado no existe.", null);

        var merchantUser = merchant.Users.FirstOrDefault();
        if (merchantUser == null)
        {
            return (true, null, null, new CommerceTransactionsResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = 0,
                TotalPages = 1,
                CommerceId = merchant.Id.ToString(),
                CommerceName = merchant.Name,
                Data = new List<CommerceTransactionItemDto>()
            });
        }

        var primaryAccount = await _unitOfWork.SavingsAccounts.GetPrimaryByClientIdAsync(merchantUser.Id);
        if (primaryAccount == null)
        {
            return (true, null, null, new CommerceTransactionsResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = 0,
                TotalPages = 1,
                CommerceId = merchant.Id.ToString(),
                CommerceName = merchant.Name,
                Data = new List<CommerceTransactionItemDto>()
            });
        }

        var transactions = await _unitOfWork.Transactions.GetBySavingsAccountIdAsync(primaryAccount.Id);
        var creditTransactions = transactions
            .Where(t => t.Type == TransactionType.CRÉDITO)
            .OrderByDescending(t => t.Date)
            .ToList();

        var totalRecords = creditTransactions.Count;
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        var pagedData = creditTransactions
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new CommerceTransactionItemDto
            {
                Id = t.Id.ToString(),
                TransactionDate = t.Date,
                Amount = t.Amount,
                CardLastFourDigits = t.Origin,
                Status = t.Status == TransactionStatus.APROBADA ? "APROBADO" : "RECHAZADO"
            })
            .ToList();

        return (true, null, null, new CommerceTransactionsResponseDto
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages > 0 ? totalPages : 1,
            CommerceId = merchant.Id.ToString(),
            CommerceName = merchant.Name,
            Data = pagedData
        });
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> ProcessPaymentAsync(Guid commerceId, ProcessPaymentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CardNumber) || dto.CardNumber.Length != 16)
            return (false, "BadRequest", "El número de tarjeta debe tener 16 dígitos.");

        if (string.IsNullOrWhiteSpace(dto.Cvc) || dto.Cvc.Length != 3)
            return (false, "BadRequest", "El CVC debe contener exactamente 3 dígitos.");

        if (dto.TransactionAmount <= 0)
            return (false, "BadRequest", "El monto de la transacción debe ser mayor que cero.");

        var merchant = await _unitOfWork.Merchants.GetByIdWithUsersAsync(commerceId);
        if (merchant == null)
            return (false, "NotFound", "El comercio indicado no existe.");

        if (merchant.Status != MerchantStatus.Activo)
            return (false, "BadRequest", "El comercio se encuentra inactivo.");

        var merchantUser = merchant.Users.FirstOrDefault();
        if (merchantUser == null)
            return (false, "BadRequest", "El comercio no tiene un usuario asociado.");

        var merchantAccount = await _unitOfWork.SavingsAccounts.GetPrimaryByClientIdAsync(merchantUser.Id);
        if (merchantAccount == null || merchantAccount.Status != AccountStatus.Activa)
            return (false, "BadRequest", "El usuario de comercio no tiene una cuenta de ahorro principal activa.");

        var card = await _unitOfWork.CreditCards.GetByCardNumberAsync(dto.CardNumber);
        if (card == null)
            return (false, "BadRequest", "La tarjeta de crédito no existe o los datos son inválidos.");

        if (card.Status != CardStatus.Activa)
            return (false, "BadRequest", "La tarjeta de crédito se encuentra cancelada.");

        // Validar expiración
        var monthStr = dto.MonthExpirationCard.PadLeft(2, '0');
        var yearStr = dto.YearExpirationCard.Length >= 2 ? dto.YearExpirationCard[^2..] : dto.YearExpirationCard;
        var expFormatted = $"{monthStr}/{yearStr}";

        if (!card.ExpirationDate.Equals(expFormatted, StringComparison.OrdinalIgnoreCase))
            return (false, "BadRequest", "Los datos de expiración de la tarjeta no coinciden.");

        // Validar fecha no vencida
        if (int.TryParse(monthStr, out var month) && int.TryParse(yearStr, out var shortYear))
        {
            var fullYear = 2000 + shortYear;
            var lastDayOfMonth = DateTime.DaysInMonth(fullYear, month);
            var expirationDateTime = new DateTime(fullYear, month, lastDayOfMonth, 23, 59, 59, DateTimeKind.Utc);
            if (DateTime.UtcNow > expirationDateTime)
                return (false, "BadRequest", "La tarjeta de crédito se encuentra vencida.");
        }

        // Validar CVC Hash
        var cvcBytes = Encoding.UTF8.GetBytes(dto.Cvc);
        var cvcHashBytes = SHA256.HashData(cvcBytes);
        var cvcBase64 = Convert.ToBase64String(cvcHashBytes);
        var cvcHex = Convert.ToHexString(cvcHashBytes);

        if (card.CvcHash != cvcBase64 && !card.CvcHash.Equals(cvcHex, StringComparison.OrdinalIgnoreCase))
            return (false, "BadRequest", "El código de seguridad CVC es incorrecto.");

        // Validar crédito disponible
        var availableCredit = card.Limit - card.Debt;
        if (dto.TransactionAmount > availableCredit)
        {
            // Registrar consumo rechazado
            var rejectedTx = new CreditCardTransaction
            {
                CreditCardId = card.Id,
                Amount = dto.TransactionAmount,
                MerchantName = merchant.Name,
                Status = CreditCardTransactionStatus.Rechazado,
                Date = DateTime.UtcNow
            };
            await _unitOfWork.CreditCardTransactions.AddAsync(rejectedTx);
            await _unitOfWork.SaveChangesAsync();

            return (false, "BadRequest", "El monto de la transacción excede el crédito disponible de la tarjeta.");
        }

        string last4 = card.CardNumber.Length >= 4 ? card.CardNumber[^4..] : card.CardNumber;
        var operationDate = DateTime.UtcNow;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            card.Debt += dto.TransactionAmount;
            _unitOfWork.CreditCards.Update(card);

            var approvedTx = new CreditCardTransaction
            {
                CreditCardId = card.Id,
                Amount = dto.TransactionAmount,
                MerchantName = merchant.Name,
                Status = CreditCardTransactionStatus.Aprobado,
                Date = operationDate
            };
            await _unitOfWork.CreditCardTransactions.AddAsync(approvedTx);

            merchantAccount.Balance += dto.TransactionAmount;
            _unitOfWork.SavingsAccounts.Update(merchantAccount);

            var accountTx = new Transaction
            {
                SavingsAccountId = merchantAccount.Id,
                Amount = dto.TransactionAmount,
                Type = TransactionType.CRÉDITO,
                Origin = last4,
                Beneficiary = merchantAccount.AccountNumber,
                Status = TransactionStatus.APROBADA,
                Date = operationDate
            };
            await _unitOfWork.Transactions.AddAsync(accountTx);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        // Enviar correos de notificación (fallo de correo no revierte la transacción)
        try
        {
            var cardOwner = await _userManager.FindByIdAsync(card.ClientId.ToString());
            if (cardOwner != null && !string.IsNullOrWhiteSpace(cardOwner.Email))
            {
                var subjectClient = $"Consumo realizado con la tarjeta {last4}";
                var bodyClient = $"""
                    <p>Hola {cardOwner.FirstName},</p>
                    <p>Se ha realizado un consumo con su tarjeta terminada en <strong>{last4}</strong>.</p>
                    <p><strong>Comercio:</strong> {merchant.Name}</p>
                    <p><strong>Monto:</strong> RD${dto.TransactionAmount:N2}</p>
                    <p><strong>Fecha y hora:</strong> {operationDate:yyyy-MM-dd HH:mm:ss}</p>
                    <p>Si usted no reconoce esta operación, comuníquese con la entidad bancaria.</p>
                    """;
                await _emailService.SendAsync(cardOwner.Email, subjectClient, bodyClient);
            }

            if (!string.IsNullOrWhiteSpace(merchant.Email))
            {
                var subjectMerchant = $"Pago recibido a través de tarjeta {last4}";
                var bodyMerchant = $"""
                    <p>Hola {merchant.Name},</p>
                    <p>Ha recibido un nuevo pago mediante Hermes Pay.</p>
                    <p><strong>Tarjeta terminada en:</strong> {last4}</p>
                    <p><strong>Monto recibido:</strong> RD${dto.TransactionAmount:N2}</p>
                    <p><strong>Fecha y hora:</strong> {operationDate:yyyy-MM-dd HH:mm:ss}</p>
                    <p>Este mensaje sirve como constancia del pago recibido.</p>
                    """;
                await _emailService.SendAsync(merchant.Email, subjectMerchant, bodyMerchant);
            }
        }
        catch
        {
            // Correo no bloquea la transacción
        }

        return (true, null, null);
    }
}
