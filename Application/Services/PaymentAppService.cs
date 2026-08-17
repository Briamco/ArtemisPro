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
                Type = TransactionType.DÉBITO,
                Status = TransactionStatus.RECHAZADA,
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
                Type = TransactionType.DÉBITO,
                Status = TransactionStatus.APROBADA,
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
                    string body = $"Hola {user.FirstName} {user.LastName},<br><br>" +
                                  $"Se ha realizado un pago a su tarjeta de crédito terminada en {cardLast4}.<br><br>" +
                                  $"Monto pagado: RD${paymentAmount:N2}<br>" +
                                  $"Cuenta origen terminada en: {accLast4}<br>" +
                                  $"Tarjeta pagada: **** **** **** {cardLast4}<br>" +
                                  $"Fecha y hora: {transactionDate:dd/MM/yyyy hh:mm:ss tt}<br><br>" +
                                  "Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";
                    await _emailService.SendAsync(user.Email, subject, body);
                }
                catch
                {
                    emailError = "El pago fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
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
                Type = TransactionType.DÉBITO,
                Status = TransactionStatus.RECHAZADA,
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
                Type = TransactionType.DÉBITO,
                Status = TransactionStatus.APROBADA,
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
                    string body = $"Hola {user.FirstName} {user.LastName},<br><br>" +
                                  $"Se ha realizado un pago a su préstamo {loan.LoanNumber}.<br><br>" +
                                  $"Monto pagado: RD${paymentAmount:N2}<br>" +
                                  $"Número del préstamo: {loan.LoanNumber}<br>" +
                                  $"Cuenta origen terminada en: {accLast4}<br>" +
                                  $"Fecha y hora: {transactionDate:dd/MM/yyyy hh:mm:ss tt}<br><br>" +
                                  "Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";
                    await _emailService.SendAsync(user.Email, subject, body);
                }
                catch
                {
                    emailError = "El pago fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
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

    public async Task<(bool Success, string? Error)> CashAdvanceAsync(CashAdvanceDto dto)
    {
        if (dto.Amount <= 0) return (false, "El monto del avance debe ser mayor que cero.");
        if (dto.CreditCardId == Guid.Empty || dto.DestinationAccountId == Guid.Empty || dto.ClientId == Guid.Empty)
            return (false, "Identificadores inválidos para procesar el avance de efectivo.");

        var card = await _unitOfWork.CreditCards.GetByIdAsync(dto.CreditCardId);
        if (card == null) return (false, "La tarjeta seleccionada no existe.");
        if (card.ClientId != dto.ClientId) return (false, "La tarjeta seleccionada no pertenece al cliente.");
        if (card.Status != CardStatus.Activa) return (false, "La tarjeta seleccionada no se encuentra activa.");
        if (IsCardExpired(card.ExpirationDate)) return (false, "La tarjeta seleccionada se encuentra vencida.");

        var destinationAccount = await _unitOfWork.SavingsAccounts.GetByIdAsync(dto.DestinationAccountId);
        if (destinationAccount == null) return (false, "La cuenta de ahorro seleccionada no existe.");
        if (destinationAccount.ClientId != dto.ClientId) return (false, "La cuenta de ahorro seleccionada no pertenece al cliente.");
        if (destinationAccount.Status != AccountStatus.Activa) return (false, "La cuenta de ahorro seleccionada no se encuentra activa.");

        decimal fee = Math.Round(dto.Amount * 0.0625m, 2);
        decimal totalCharge = dto.Amount + fee;
        decimal availableCredit = card.Limit - card.Debt;

        string cardLast4 = card.CardNumber.Length >= 4 ? card.CardNumber.Substring(card.CardNumber.Length - 4) : card.CardNumber;

        if (totalCharge > availableCredit)
        {
            var failedCardTx = new CreditCardTransaction
            {
                CreditCardId = card.Id,
                Amount = totalCharge,
                MerchantName = "AVANCE",
                Status = CreditCardTransactionStatus.Rechazado,
                Date = DateTime.UtcNow
            };
            await _unitOfWork.CreditCardTransactions.AddAsync(failedCardTx);
            await _unitOfWork.SaveChangesAsync();
            return (false, "El avance solicitado excede el crédito disponible de la tarjeta seleccionada.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            destinationAccount.Balance += dto.Amount;
            card.Debt += totalCharge;

            var transactionDate = DateTime.UtcNow;

            var creditTransaction = new Transaction
            {
                SavingsAccountId = destinationAccount.Id,
                Amount = dto.Amount,
                Type = TransactionType.CRÉDITO,
                Status = TransactionStatus.APROBADA,
                Beneficiary = destinationAccount.AccountNumber,
                Origin = cardLast4,
                Date = transactionDate
            };
            await _unitOfWork.Transactions.AddAsync(creditTransaction);

            var cardTransaction = new CreditCardTransaction
            {
                CreditCardId = card.Id,
                Amount = totalCharge,
                MerchantName = "AVANCE",
                Status = CreditCardTransactionStatus.Aprobado,
                Date = transactionDate
            };
            await _unitOfWork.CreditCardTransactions.AddAsync(cardTransaction);

            _unitOfWork.SavingsAccounts.Update(destinationAccount);
            _unitOfWork.CreditCards.Update(card);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            string? emailError = null;
            var user = (await _unitOfWork.Users.FindAsync(u => u.Id == dto.ClientId)).FirstOrDefault();
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    string destLast4 = destinationAccount.AccountNumber.Length >= 4
                        ? destinationAccount.AccountNumber.Substring(destinationAccount.AccountNumber.Length - 4)
                        : destinationAccount.AccountNumber;

                    string subject = $"Avance de efectivo desde la tarjeta {cardLast4}";
                    string body = $"Hola {user.FirstName} {user.LastName},<br><br>" +
                                  $"Se ha realizado un avance de efectivo desde su tarjeta terminada en {cardLast4}.<br><br>" +
                                  $"Monto depositado: RD${dto.Amount:N2}<br>" +
                                  $"Interés aplicado: RD${fee:N2}<br>" +
                                  $"Total cargado a la tarjeta: RD${totalCharge:N2}<br>" +
                                  $"Cuenta destino terminada en: {destLast4}<br>" +
                                  $"Fecha y hora: {transactionDate:dd/MM/yyyy hh:mm:ss tt}<br><br>" +
                                  "Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";

                    await _emailService.SendAsync(user.Email, subject, body);
                }
                catch
                {
                    emailError = "El avance fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
                }
            }

            return (true, emailError);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            return (false, "Ocurrió un error al procesar el avance de efectivo.");
        }
    }

    private static bool IsCardExpired(string expirationDate)
    {
        if (string.IsNullOrWhiteSpace(expirationDate)) return false;
        var parts = expirationDate.Split('/');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var month) || !int.TryParse(parts[1], out var yearShort))
            return false;

        var year = 2000 + yearShort;
        var lastDayOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);
        return DateTime.UtcNow > lastDayOfMonth;
    }
}
