using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Shared.Interfaces;

namespace Application.Services;

public class LoanAppService : ILoanAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;

    public LoanAppService(IUnitOfWork unitOfWork, IMapper mapper, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
    }

    public async Task<IEnumerable<LoanDto>> GetLoansAsync(string? status = null, string? cedula = null)
    {
        var loansQuery = _unitOfWork.Loans.GetAllAsync().AsQueryable();
        loansQuery = loansQuery.Include(l => l.Client).Include(l => l.Installments);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<LoanStatus>(status, out var loanStatus))
        {
            loansQuery = loansQuery.Where(l => l.Status == loanStatus);
        }

        if (!string.IsNullOrEmpty(cedula))
        {
            var user = (await _unitOfWork.Users.FindAsync(u => u.Cedula == cedula)).FirstOrDefault();
            if (user == null)
            {
                return Enumerable.Empty<LoanDto>();
            }
            loansQuery = loansQuery.Where(l => l.ClientId == user.Id);
        }

        var loans = await loansQuery.ToListAsync();
        return _mapper.Map<IEnumerable<LoanDto>>(loans);
    }

    public async Task<LoanDto?> GetLoanByIdAsync(Guid id)
    {
        var loan = await _unitOfWork.Loans.GetByIdAsync(id);
        return loan == null ? null : _mapper.Map<LoanDto>(loan);
    }

    public async Task<IEnumerable<LoanInstallmentDto>> GetInstallmentsAsync(Guid loanId)
    {
        var installments = await _unitOfWork.LoanInstallments.FindAsync(i => i.LoanId == loanId);
        return _mapper.Map<IEnumerable<LoanInstallmentDto>>(installments.OrderBy(i => i.InstallmentNumber));
    }

    public async Task<LoanCreationResult> CreateLoanAsync(CreateLoanDto dto)
    {
        var client = await _unitOfWork.Users.GetByIdAsync(dto.ClientId);
        if (client == null)
            return new LoanCreationResult { Success = false, ErrorMessage = "Cliente no encontrado." };

        if (!client.IsActive)
            return new LoanCreationResult { Success = false, ErrorMessage = "El cliente no está activo." };

        var activeLoan = await _unitOfWork.Loans.GetActiveByClientIdAsync(dto.ClientId);
        if (activeLoan != null)
            return new LoanCreationResult { Success = false, ErrorMessage = "Este cliente ya tiene un préstamo activo asignado." };

        var allowedTerms = new[] { 6, 12, 18, 24, 30, 36, 42, 48, 54, 60 };
        if (!allowedTerms.Contains(dto.Term))
            return new LoanCreationResult { Success = false, ErrorMessage = "El plazo seleccionado no es válido." };

        var primaryAccount = await _unitOfWork.SavingsAccounts.GetPrimaryByClientIdAsync(dto.ClientId);
        if (primaryAccount == null || primaryAccount.Status != AccountStatus.Activa)
            return new LoanCreationResult { Success = false, ErrorMessage = "El cliente no tiene una cuenta de ahorro principal activa para recibir el desembolso del préstamo." };

        // 1. Calculate Average Debt of the System
        var (averageDebt, hasClients) = await GetAverageDebtAsync();

        // 2. Calculate Current Debt of the Client
        var currentLoansDebt = await _unitOfWork.LoanInstallments.GetTotalPendingDebtByClientIdAsync(dto.ClientId);
        var currentCardsDebt = await _unitOfWork.CreditCards.GetTotalActiveDebtByClientIdAsync(dto.ClientId);

        var currentDebt = currentLoansDebt + currentCardsDebt;

        // 3. Calculate Projected Debt
        decimal monthlyInterestRate = (dto.AnnualInterestRate / 100m) / 12m;
        decimal baseMonthlyPayment = 0;

        if (monthlyInterestRate > 0)
        {
            var factor = (decimal)Math.Pow((double)(1 + monthlyInterestRate), dto.Term);
            baseMonthlyPayment = Math.Round(dto.ApprovedAmount * (monthlyInterestRate * factor) / (factor - 1), 2);
        }
        else
        {
            baseMonthlyPayment = Math.Round(dto.ApprovedAmount / dto.Term, 2);
        }

        decimal totalToPayNewLoan = 0;
        decimal tempBalance = dto.ApprovedAmount;
        for (int i = 1; i <= dto.Term; i++)
        {
            decimal interestAmount = Math.Round(tempBalance * monthlyInterestRate, 2);
            decimal capitalAmount = Math.Round(baseMonthlyPayment - interestAmount, 2);
            decimal paymentAmount = baseMonthlyPayment;

            if (i == dto.Term)
            {
                capitalAmount = Math.Round(tempBalance, 2);
                paymentAmount = Math.Round(capitalAmount + interestAmount, 2);
            }

            tempBalance -= capitalAmount;
            totalToPayNewLoan += paymentAmount;
        }

        var projectedDebt = currentDebt + totalToPayNewLoan;

        // 4. Validate High Risk
        if (hasClients && !dto.ConfirmHighRisk)
        {
            if (currentDebt > averageDebt)
            {
                return new LoanCreationResult
                {
                    Success = false,
                    IsHighRiskConflict = true,
                    RiskType = "CurrentHighRisk",
                    CurrentDebt = currentDebt,
                    ProjectedDebt = projectedDebt,
                    AverageDebt = averageDebt,
                    ErrorMessage = "Este cliente se considera de alto riesgo, ya que su deuda actual supera el promedio del sistema."
                };
            }

            if (projectedDebt > averageDebt)
            {
                return new LoanCreationResult
                {
                    Success = false,
                    IsHighRiskConflict = true,
                    RiskType = "ProjectedHighRisk",
                    CurrentDebt = currentDebt,
                    ProjectedDebt = projectedDebt,
                    AverageDebt = averageDebt,
                    ErrorMessage = "Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, ya que su deuda superará el umbral promedio del sistema."
                };
            }
        }

        // Proceed to create loan
        await _unitOfWork.BeginTransactionAsync();
        Loan loan = null;
        try
        {
            var loanNumber = await GenerateUniqueLoanNumberAsync();
            loan = new Loan
            {
                ClientId = dto.ClientId,
                LoanNumber = loanNumber,
                ApprovedAmount = dto.ApprovedAmount,
                Term = dto.Term,
                AnnualInterestRate = dto.AnnualInterestRate,
                Status = LoanStatus.Activo,
                CreatedAt = DateTime.UtcNow,
                AdminId = dto.AdminId
            };

            await _unitOfWork.Loans.AddAsync(loan);

            // Generate Installments
            await GenerateInstallmentsAsync(loan, monthlyInterestRate, baseMonthlyPayment);
            
            // Credit approved amount to primary savings account
            primaryAccount.Balance += dto.ApprovedAmount;
            _unitOfWork.SavingsAccounts.Update(primaryAccount);

            var transaction = new Transaction
            {
                SavingsAccountId = primaryAccount.Id,
                Amount = dto.ApprovedAmount,
                Type = TransactionType.Credito,
                Status = TransactionStatus.Aprobada,
                Date = DateTime.UtcNow,
                Origin = "Desembolso de Préstamo",
                Beneficiary = $"{client.FirstName} {client.LastName}"
            };
            
            await _unitOfWork.Transactions.AddAsync(transaction);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        // Send email notification
        var subject = "Préstamo aprobado";
        var body = $"Estimado/a {client.FirstName} {client.LastName},<br><br>Su préstamo número {loan.LoanNumber} por un monto de {dto.ApprovedAmount:C} ha sido aprobado y acreditado a su cuenta principal.<br>Plazo: {dto.Term} meses<br>Tasa de interés anual: {dto.AnnualInterestRate}%<br>Cuota mensual: {baseMonthlyPayment:C}<br><br>Gracias por confiar en nosotros.";
        try
        {
            if (!string.IsNullOrEmpty(client.Email))
            {
                await _emailService.SendAsync(client.Email, subject, body);
            }
        }
        catch (Exception)
        {
            // Se silencia la excepción para no abortar la creación del préstamo
            // si el servicio de mensajería falla.
        }

        return new LoanCreationResult { Success = true };
    }

    public async Task<(bool Success, string? Error)> UpdateLoanRateAsync(Guid id, UpdateLoanRateDto dto)
    {
        var loan = await _unitOfWork.Loans.GetByIdAsync(id);
        if (loan == null) return (false, "Préstamo no encontrado.");
        if (loan.Status != LoanStatus.Activo) return (false, "Solo se puede modificar la tasa de interés de préstamos activos.");

        loan.AnnualInterestRate = dto.AnnualInterestRate;
        _unitOfWork.Loans.Update(loan);

        var installments = await _unitOfWork.LoanInstallments.FindAsync(i => i.LoanId == id);
        var futureInstallments = installments
            .Where(i => i.DueDate > DateTime.UtcNow && i.PaymentStatus == PaymentStatus.Pendiente)
            .OrderBy(i => i.InstallmentNumber)
            .ToList();

        if (futureInstallments.Any())
        {
            decimal pendingPrincipal = futureInstallments.Sum(i => i.CapitalAmount);
            decimal monthlyInterestRate = (dto.AnnualInterestRate / 100m) / 12m;
            int remainingTerm = futureInstallments.Count;

            decimal newMonthlyPayment = 0;
            if (monthlyInterestRate > 0)
            {
                var factor = (decimal)Math.Pow((double)(1 + monthlyInterestRate), remainingTerm);
                newMonthlyPayment = Math.Round(pendingPrincipal * (monthlyInterestRate * factor) / (factor - 1), 2);
            }
            else
            {
                newMonthlyPayment = Math.Round(pendingPrincipal / remainingTerm, 2);
            }

            decimal tempBalance = pendingPrincipal;
            for (int i = 0; i < remainingTerm; i++)
            {
                var inst = futureInstallments[i];
                decimal interestAmount = Math.Round(tempBalance * monthlyInterestRate, 2);
                decimal capitalAmount = Math.Round(newMonthlyPayment - interestAmount, 2);
                decimal paymentAmount = newMonthlyPayment;

                if (i == remainingTerm - 1) // Adjust last payment to fix decimals
                {
                    capitalAmount = Math.Round(tempBalance, 2);
                    paymentAmount = Math.Round(capitalAmount + interestAmount, 2);
                }

                tempBalance -= capitalAmount;

                inst.Amount = paymentAmount;
                inst.InterestAmount = interestAmount;
                inst.CapitalAmount = capitalAmount;
                inst.PendingBalance = paymentAmount; // Pending amount is full for these
                
                _unitOfWork.LoanInstallments.Update(inst);
            }
        }
        else
        {
            // No future installments to recalculate
            return (false, "No existen cuotas futuras pendientes para recalcular.");
        }

        await _unitOfWork.SaveChangesAsync();

        var client = await _unitOfWork.Users.GetByIdAsync(loan.ClientId);
        if (client != null && !string.IsNullOrEmpty(client.Email))
        {
            var subject = "Actualización de Tasa de Interés";
            var body = $"Estimado/a {client.FirstName} {client.LastName},<br><br>Le informamos que la tasa de interés de su préstamo {loan.LoanNumber} ha sido actualizada a {dto.AnnualInterestRate}%. Sus cuotas futuras han sido recalculadas.<br><br>Atentamente,<br>Artemis Banking Pro";
            try
            {
                await _emailService.SendAsync(client.Email, subject, body);
            }
            catch (Exception)
            {
                // Silence exception
            }
        }

        return (true, null);
    }

    public async Task<(decimal AverageDebt, bool HasClients)> GetAverageDebtAsync()
    {
        var activeClientsCount = await _unitOfWork.GetActiveClientsCountAsync();

        if (activeClientsCount == 0)
        {
            return (0, false);
        }

        decimal totalInstallmentsDebt = await _unitOfWork.LoanInstallments.GetTotalSystemPendingDebtAsync();
        decimal totalCardsDebt = await _unitOfWork.CreditCards.GetTotalSystemActiveDebtAsync();

        decimal totalDebt = totalInstallmentsDebt + totalCardsDebt;

        return (totalDebt / activeClientsCount, true);
    }

    private async Task<string> GenerateUniqueLoanNumberAsync()
    {
        var random = new Random();
        string number;
        bool isUnique = false;
        
        while (!isUnique)
        {
            number = random.Next(100000000, 1000000000).ToString();
            var existingLoan = await _unitOfWork.Loans.FindAsync(l => l.LoanNumber == number);
            var existingAccount = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(number);
            
            if (!existingLoan.Any() && existingAccount == null)
            {
                isUnique = true;
                return number;
            }
        }
        
        return string.Empty;
    }

    private async Task GenerateInstallmentsAsync(Loan loan, decimal monthlyInterestRate, decimal monthlyPayment)
    {
        decimal pendingBalance = loan.ApprovedAmount;
        var installments = new List<LoanInstallment>();

        for (int i = 1; i <= loan.Term; i++)
        {
            decimal interestAmount = Math.Round(pendingBalance * monthlyInterestRate, 2);
            decimal capitalAmount = Math.Round(monthlyPayment - interestAmount, 2);
            decimal paymentAmount = monthlyPayment;

            if (i == loan.Term) // Adjust last payment to fix decimals
            {
                capitalAmount = Math.Round(pendingBalance, 2);
                paymentAmount = Math.Round(capitalAmount + interestAmount, 2);
            }

            pendingBalance -= capitalAmount;

            var installment = new LoanInstallment
            {
                LoanId = loan.Id,
                InstallmentNumber = i,
                DueDate = loan.CreatedAt.AddMonths(i),
                Amount = paymentAmount,
                InterestAmount = interestAmount,
                CapitalAmount = capitalAmount,
                PendingBalance = paymentAmount, // Monto pendiente por pagar de esa cuota
                PaymentStatus = PaymentStatus.Pendiente,
                IsOverdue = false
            };

            installments.Add(installment);
        }

        foreach(var inst in installments)
        {
            await _unitOfWork.LoanInstallments.AddAsync(inst);
        }
    }
}
