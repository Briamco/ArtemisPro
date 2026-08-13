using System;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Shared.Interfaces;

namespace Application.Services;

public class PaymentAppService : IPaymentAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public PaymentAppService(IUnitOfWork unitOfWork, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    public async Task<(bool Success, string? Error)> PayCreditCardAsync(PayCreditCardDto dto)
    {
        if (dto.Amount <= 0) return (false, "El monto a pagar debe ser mayor que cero.");
        if (dto.SourceAccountId == Guid.Empty || dto.CreditCardId == Guid.Empty || dto.ClientId == Guid.Empty)
            return (false, "Identificadores inválidos para procesar el pago.");

        var sourceAccount = await _unitOfWork.SavingsAccounts.GetByIdAsync(dto.SourceAccountId);
        if (sourceAccount == null) return (false, "La cuenta de origen no existe.");
        if (sourceAccount.ClientId != dto.ClientId) return (false, "La cuenta de origen no pertenece al cliente.");
        if (sourceAccount.Status != AccountStatus.Activa) return (false, "La cuenta de origen debe estar activa.");

        var card = await _unitOfWork.CreditCards.GetByIdAsync(dto.CreditCardId);
        if (card == null) return (false, "La tarjeta seleccionada no existe.");
        if (card.ClientId != dto.ClientId) return (false, "La tarjeta seleccionada no pertenece al cliente.");
        if (card.Status != CardStatus.Activa) return (false, "La tarjeta seleccionada debe estar activa.");
        if (card.Debt <= 0) return (false, "La tarjeta seleccionada no tiene deuda pendiente.");

        var paymentAmount = Math.Min(dto.Amount, card.Debt);
        string cardLast4 = card.CardNumber.Length >= 4 ? card.CardNumber.Substring(card.CardNumber.Length - 4) : card.CardNumber;
        string accLast4 = sourceAccount.AccountNumber.Length >= 4 ? sourceAccount.AccountNumber.Substring(sourceAccount.AccountNumber.Length - 4) : sourceAccount.AccountNumber;

        if (sourceAccount.Balance < paymentAmount)
        {
            var failedTx = new Transaction
            {
                SavingsAccountId = sourceAccount.Id,
                Amount = paymentAmount,
                Type = TransactionType.Debito,
                Status = TransactionStatus.Rechazada,
                Beneficiary = cardLast4,
                Origin = sourceAccount.AccountNumber,
                Date = DateTime.UtcNow
            };
            await _unitOfWork.Transactions.AddAsync(failedTx);
            await _unitOfWork.SaveChangesAsync();
            return (false, "No dispone del monto requerido en la cuenta seleccionada.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            sourceAccount.Balance -= paymentAmount;
            card.Debt -= paymentAmount;

            var transactionDate = DateTime.UtcNow;

            var transaction = new Transaction
            {
                SavingsAccountId = sourceAccount.Id,
                Amount = paymentAmount,
                Type = TransactionType.Debito,
                Status = TransactionStatus.Aprobada,
                Beneficiary = cardLast4,
                Origin = sourceAccount.AccountNumber,
                Date = transactionDate
            };
            await _unitOfWork.Transactions.AddAsync(transaction);

            var cardTransaction = new CreditCardTransaction
            {
                CreditCardId = card.Id,
                Amount = paymentAmount,
                MerchantName = "Pago de tarjeta",
                Status = CreditCardTransactionStatus.Aprobado,
                Date = transactionDate
            };
            await _unitOfWork.CreditCardTransactions.AddAsync(cardTransaction);

            _unitOfWork.SavingsAccounts.Update(sourceAccount);
            _unitOfWork.CreditCards.Update(card);
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            string? emailError = null;
            var user = (await _unitOfWork.Users.FindAsync(u => u.Id == sourceAccount.ClientId)).FirstOrDefault();
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    string subject = $"Pago realizado a la tarjeta {cardLast4}";
                    string body = $"Monto pagado: {paymentAmount}\n" +
                                  $"Últimos cuatro dígitos de la cuenta desde la cual se realizó el pago: {accLast4}\n" +
                                  $"Últimos cuatro dígitos de la tarjeta pagada: {cardLast4}\n" +
                                  $"Fecha de la transacción: {transactionDate:yyyy-MM-dd}\n" +
                                  $"Hora exacta de la transacción: {transactionDate:HH:mm:ss}";
                    await _emailService.SendAsync(user.Email, subject, body);
                }
                catch
                {
                    emailError = "El pago fue procesado correctamente, pero ocurrió un error al enviar el correo de notificación.";
                }
            }

            return (true, emailError);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            return (false, "Ocurrió un error al procesar el pago.");
        }
    }

    public async Task<(bool Success, string? Error)> PayLoanAsync(PayLoanDto dto)
    {
        if (dto.Amount <= 0) return (false, "El monto a pagar debe ser mayor que cero.");
        if (dto.SourceAccountId == Guid.Empty || dto.LoanId == Guid.Empty || dto.ClientId == Guid.Empty)
            return (false, "Identificadores inválidos para procesar el pago.");

        var sourceAccount = await _unitOfWork.SavingsAccounts.GetByIdAsync(dto.SourceAccountId);
        if (sourceAccount == null) return (false, "La cuenta de origen no existe.");
        if (sourceAccount.ClientId != dto.ClientId) return (false, "La cuenta de origen no pertenece al cliente.");
        if (sourceAccount.Status != AccountStatus.Activa) return (false, "La cuenta de origen debe estar activa.");

        var loan = await _unitOfWork.Loans.GetByIdAsync(dto.LoanId);
        if (loan == null) return (false, "El préstamo seleccionado no existe.");
        if (loan.ClientId != dto.ClientId) return (false, "El préstamo seleccionado no pertenece al cliente.");
        if (loan.Status != LoanStatus.Activo) return (false, "El préstamo seleccionado debe estar activo.");

        var installments = await _unitOfWork.LoanInstallments.FindAsync(i => i.LoanId == loan.Id);
        var pendingInstallments = installments.Where(i => i.PaymentStatus != PaymentStatus.Pagada).OrderBy(i => i.DueDate).ToList();
        
        if (!pendingInstallments.Any()) return (false, "El préstamo seleccionado no tiene cuotas pendientes de pago.");

        var totalPending = pendingInstallments.Sum(i => i.PendingBalance);
        var paymentAmount = Math.Min(dto.Amount, totalPending);
        string accLast4 = sourceAccount.AccountNumber.Length >= 4 ? sourceAccount.AccountNumber.Substring(sourceAccount.AccountNumber.Length - 4) : sourceAccount.AccountNumber;

        if (sourceAccount.Balance < paymentAmount)
        {
            var failedTx = new Transaction
            {
                SavingsAccountId = sourceAccount.Id,
                Amount = paymentAmount,
                Type = TransactionType.Debito,
                Status = TransactionStatus.Rechazada,
                Beneficiary = loan.LoanNumber,
                Origin = sourceAccount.AccountNumber,
                Date = DateTime.UtcNow
            };
            await _unitOfWork.Transactions.AddAsync(failedTx);
            await _unitOfWork.SaveChangesAsync();
            return (false, "No dispone del monto requerido en la cuenta seleccionada.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            sourceAccount.Balance -= paymentAmount;
            var transactionDate = DateTime.UtcNow;
            
            var transaction = new Transaction
            {
                SavingsAccountId = sourceAccount.Id,
                Amount = paymentAmount,
                Type = TransactionType.Debito,
                Status = TransactionStatus.Aprobada,
                Beneficiary = loan.LoanNumber,
                Origin = sourceAccount.AccountNumber,
                Date = transactionDate
            };
            await _unitOfWork.Transactions.AddAsync(transaction);

            decimal remainingPayment = paymentAmount;
            foreach (var inst in pendingInstallments)
            {
                if (remainingPayment <= 0) break;

                if (remainingPayment >= inst.PendingBalance)
                {
                    remainingPayment -= inst.PendingBalance;
                    inst.PendingBalance = 0;
                    inst.PaymentStatus = PaymentStatus.Pagada;
                    inst.IsOverdue = false;
                }
                else
                {
                    inst.PendingBalance -= remainingPayment;
                    inst.PaymentStatus = PaymentStatus.Parcial;
                    remainingPayment = 0;
                }
                _unitOfWork.LoanInstallments.Update(inst);
            }

            if (!installments.Any(i => i.PaymentStatus != PaymentStatus.Pagada))
            {
                loan.Status = LoanStatus.Completado;
                _unitOfWork.Loans.Update(loan);
            }

            _unitOfWork.SavingsAccounts.Update(sourceAccount);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            string? emailError = null;
            var user = (await _unitOfWork.Users.FindAsync(u => u.Id == sourceAccount.ClientId)).FirstOrDefault();
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    string subject = $"Pago realizado al préstamo {loan.LoanNumber}";
                    string body = $"Monto pagado: {paymentAmount}\n" +
                                  $"Número del préstamo: {loan.LoanNumber}\n" +
                                  $"Últimos cuatro dígitos de la cuenta desde la cual se realizó el pago: {accLast4}\n" +
                                  $"Fecha de la transacción: {transactionDate:yyyy-MM-dd}\n" +
                                  $"Hora exacta de la transacción: {transactionDate:HH:mm:ss}";
                    await _emailService.SendAsync(user.Email, subject, body);
                }
                catch
                {
                    emailError = "El pago fue procesado correctamente, pero ocurrió un error al enviar el correo de notificación.";
                }
            }

            return (true, emailError);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            return (false, "Ocurrió un error al procesar el pago.");
        }
    }
}
