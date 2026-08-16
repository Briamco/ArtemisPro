using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Shared.Interfaces;

namespace Application.Services.Banking;

public class LoanPaymentAppService : BankingPaymentServiceBase, ILoanPaymentAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoanPaymentAppService> _logger;

    public LoanPaymentAppService(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<LoanPaymentAppService> logger)
        : base(emailService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<LoanPaymentPreviewResult> GetLoanPaymentPreviewAsync(string accountNumber, string loanNumber, decimal amount)
    {
        if (amount <= 0)
        {
            return PreviewFailed("El monto a pagar debe ser mayor que cero.");
        }

        if (string.IsNullOrWhiteSpace(loanNumber) || loanNumber.Length != 9)
        {
            return PreviewFailed("El número del préstamo debe contener 9 dígitos.");
        }

        var account = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(accountNumber);
        if (account == null || account.Status != AccountStatus.Activa)
        {
            return PreviewFailed("El número de cuenta ingresado no corresponde a una cuenta válida.");
        }

        var loan = await _unitOfWork.Loans.GetByLoanNumberAsync(loanNumber);
        if (loan == null || loan.Status != LoanStatus.Activo)
        {
            return PreviewFailed("El número de préstamo ingresado no corresponde a un préstamo válido.");
        }

        var pendingInstallments = await GetPendingInstallmentsAsync(loan.Id);
        if (!pendingInstallments.Any())
        {
            return PreviewFailed("El préstamo seleccionado no tiene cuotas pendientes de pago.");
        }

        var totalPending = pendingInstallments.Sum(i => i.PendingBalance);
        var effectiveAmount = Math.Min(amount, totalPending);

        if (account.Balance < effectiveAmount)
        {
            return PreviewFailed("El monto ingresado excede el saldo disponible de la cuenta.");
        }

        return new LoanPaymentPreviewResult
        {
            Success = true,
            Preview = new LoanPaymentPreviewDto
            {
                OriginAccountNumber = account.AccountNumber,
                OriginAccountClientName = BuildOwnerName(account.Client),
                LoanNumber = loan.LoanNumber,
                LoanClientName = BuildOwnerName(loan.Client),
                EnteredAmount = amount,
                EffectiveAmount = effectiveAmount
            }
        };
    }

    public async Task<LoanPaymentResult> CreateLoanPaymentAsync(Guid tellerId, CreateLoanPaymentDto dto)
    {
        if (dto.Amount <= 0)
        {
            return Failed("El monto a pagar debe ser mayor que cero.");
        }

        if (string.IsNullOrWhiteSpace(dto.LoanNumber) || dto.LoanNumber.Length != 9)
        {
            return Failed("El número del préstamo debe contener 9 dígitos.");
        }

        var account = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(dto.AccountNumber);
        if (account == null || account.Status != AccountStatus.Activa)
        {
            // No se registra una transacción RECHAZADA en este caso porque el registro requiere una
            // cuenta válida para vincularlo (SavingsAccountId y Origin). Esto es consistente con el
            // patrón de CardPaymentAppService y ThirdPartyTransactionAppService, donde la cuenta
            // origen inexistente tampoco genera un registro de rechazo.
            return Failed("El número de cuenta ingresado no corresponde a una cuenta válida.");
        }

        var loan = await _unitOfWork.Loans.GetByLoanNumberAsync(dto.LoanNumber);
        if (loan == null || loan.Status != LoanStatus.Activo)
        {
            return await RejectAsync(account, dto.LoanNumber, dto.Amount, tellerId, "El número de préstamo ingresado no corresponde a un préstamo válido.");
        }

        var pendingInstallments = (await GetPendingInstallmentsAsync(loan.Id)).OrderBy(i => i.DueDate).ThenBy(i => i.InstallmentNumber).ToList();
        if (!pendingInstallments.Any())
        {
            return await RejectAsync(account, dto.LoanNumber, dto.Amount, tellerId, "El préstamo seleccionado no tiene cuotas pendientes de pago.");
        }

        var totalPending = pendingInstallments.Sum(i => i.PendingBalance);
        var effectiveAmount = Math.Min(dto.Amount, totalPending);

        if (account.Balance < effectiveAmount)
        {
            return await RejectAsync(account, dto.LoanNumber, effectiveAmount, tellerId, "El monto ingresado excede el saldo disponible de la cuenta.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            account.Balance -= effectiveAmount;
            _unitOfWork.SavingsAccounts.Update(account);

            var remaining = effectiveAmount;
            foreach (var installment in pendingInstallments)
            {
                if (remaining <= 0) break;

                if (remaining >= installment.PendingBalance)
                {
                    remaining -= installment.PendingBalance;
                    installment.PendingBalance = 0;
                    installment.PaymentStatus = PaymentStatus.Pagada;
                    installment.IsOverdue = false;
                }
                else
                {
                    installment.PendingBalance -= remaining;
                    installment.PaymentStatus = PaymentStatus.Parcial;
                    remaining = 0;
                }

                _unitOfWork.LoanInstallments.Update(installment);
            }

            var allInstallmentsPaid = pendingInstallments.All(i => i.PaymentStatus == PaymentStatus.Pagada);
            if (allInstallmentsPaid)
            {
                loan.Status = LoanStatus.Completado;
                _unitOfWork.Loans.Update(loan);
            }

            var transaction = new Transaction
            {
                SavingsAccountId = account.Id,
                Amount = effectiveAmount,
                Type = TransactionType.DÉBITO,
                Origin = account.AccountNumber,
                Beneficiary = loan.LoanNumber,
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
            _logger.LogError(ex, "Error al procesar el pago a préstamo. Cuenta origen: {AccountNumber}, Préstamo: {LoanNumber}, Monto: {Amount}, Cajero: {TellerId}",
                account.AccountNumber, dto.LoanNumber, effectiveAmount, tellerId);
            return Failed("Ocurrió un error al realizar el pago.");
        }

        var emailSent = await SendPaymentNotificationAsync(account, loan, effectiveAmount);

        return new LoanPaymentResult { Success = true, EmailSent = emailSent };
    }

    private async Task<IEnumerable<LoanInstallment>> GetPendingInstallmentsAsync(Guid loanId)
    {
        return await _unitOfWork.LoanInstallments.FindAsync(i => i.LoanId == loanId && i.PaymentStatus != PaymentStatus.Pagada);
    }

    private async Task<LoanPaymentResult> RejectAsync(SavingsAccount account, string loanNumber, decimal amount, Guid tellerId, string error)
    {
        var rejected = new Transaction
        {
            SavingsAccountId = account.Id,
            Amount = amount,
            Type = TransactionType.DÉBITO,
            Origin = account.AccountNumber,
            Beneficiary = loanNumber,
            Status = TransactionStatus.RECHAZADA,
            Date = DateTime.UtcNow,
            PerformedById = tellerId
        };

        await _unitOfWork.Transactions.AddAsync(rejected);
        await _unitOfWork.SaveChangesAsync();

        return Failed(error);
    }

    private async Task<bool> SendPaymentNotificationAsync(SavingsAccount account, Loan loan, decimal amount)
    {
        // SendAsync retorna true cuando no hay email que enviar (cliente o email ausente);
        // la ausencia de notificación no se considera un fallo del pago.
        var loanOwnerEmailSent = await SendAsync(
            loan.Client?.Email,
            $"Pago realizado al préstamo {loan.LoanNumber}",
            BuildLoanOwnerBody(loan, account, amount));

        var accountOwnerEmailSent = true;
        if (account.ClientId != loan.ClientId)
        {
            accountOwnerEmailSent = await SendAsync(
                account.Client?.Email,
                $"Débito realizado desde su cuenta {GetLast4(account.AccountNumber)}",
                BuildAccountOwnerBody(account, loan, amount));
        }

        return loanOwnerEmailSent && accountOwnerEmailSent;
    }

    private string BuildLoanOwnerBody(Loan loan, SavingsAccount account, decimal amount)
    {
        return $"Hola {BuildOwnerName(loan.Client)},<br><br>" +
               $"Se ha realizado un pago a su préstamo {loan.LoanNumber}.<br><br>" +
               $"Monto pagado: RD${amount:N2}<br>" +
               $"Número del préstamo: {loan.LoanNumber}<br>" +
               $"Cuenta origen terminada en: {GetLast4(account.AccountNumber)}<br>" +
               $"Fecha y hora: {DateTime.UtcNow:dd/MM/yyyy hh:mm:ss tt}<br><br>" +
               "Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";
    }

    private string BuildAccountOwnerBody(SavingsAccount account, Loan loan, decimal amount)
    {
        return $"Hola {BuildOwnerName(account.Client)},<br><br>" +
               $"Se ha realizado un débito de RD${amount:N2} desde su cuenta terminada en {GetLast4(account.AccountNumber)} " +
               $"para realizar un pago al préstamo {loan.LoanNumber}.<br><br>" +
               $"Fecha y hora: {DateTime.UtcNow:dd/MM/yyyy hh:mm:ss tt}<br><br>" +
               "Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";
    }

    private LoanPaymentResult Failed(string error)
    {
        return new LoanPaymentResult { Success = false, Error = error, EmailSent = false };
    }

    private LoanPaymentPreviewResult PreviewFailed(string error)
    {
        return new LoanPaymentPreviewResult { Success = false, Error = error };
    }
}
