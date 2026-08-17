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

    public async Task<PagedResultDto<LoanDto>> GetLoansAsync(int page, int pageSize, string? status = null, string? identification = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 20) pageSize = 20;

        IQueryable<Loan> query = _unitOfWork.Loans.Query()
            .Include(l => l.Client)
            .Include(l => l.Installments);

        if (!string.IsNullOrEmpty(identification))
        {
            var user = (await _unitOfWork.Users.FindAsync(u => u.Cedula == identification)).FirstOrDefault();
            if (user == null)
            {
                return new PagedResultDto<LoanDto> { Page = page, PageSize = pageSize, TotalRecords = 0, TotalPages = 0, Data = Enumerable.Empty<LoanDto>() };
            }
            query = query.Where(l => l.ClientId == user.Id);
        }

        if (string.IsNullOrEmpty(status) || status.ToLower() == "activos")
        {
            query = query.Where(l => l.Status == LoanStatus.Activo);
        }
        else if (status.ToLower() == "completados")
        {
            query = query.Where(l => l.Status == LoanStatus.Completado);
        }
        else if (status.ToLower() != "todos")
        {
            return new PagedResultDto<LoanDto> { Page = page, PageSize = pageSize, TotalRecords = 0, TotalPages = 0, Data = Enumerable.Empty<LoanDto>() };
        }

        query = query.OrderByDescending(l => l.CreatedAt);

        var totalRecords = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

        var loans = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<LoanDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            Data = _mapper.Map<IEnumerable<LoanDto>>(loans)
        };
    }

    public async Task<LoanDetailDto?> GetLoanByIdAsync(Guid id)
    {
        var loan = await _unitOfWork.Loans.Query()
            .Include(l => l.Client)
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan == null) return null;

        return _mapper.Map<LoanDetailDto>(loan);
    }

    public async Task<LoanCreationResponseDto?> CreateLoanAsync(CreateLoanDto dto, Guid adminId)
    {
        var client = await _unitOfWork.Users.GetByIdAsync(dto.ClientId);
        if (client == null)
            throw new InvalidOperationException("Cliente no encontrado.");

        if (!client.IsActive)
            throw new InvalidOperationException("El cliente no está activo.");

        var activeLoan = await _unitOfWork.Loans.GetActiveByClientIdAsync(dto.ClientId);
        if (activeLoan != null)
            throw new InvalidOperationException("Este cliente ya tiene un préstamo activo asignado.");

        var allowedTerms = new[] { 6, 12, 18, 24, 30, 36, 42, 48, 54, 60 };
        if (!allowedTerms.Contains(dto.TermInMonths))
            throw new InvalidOperationException("El plazo seleccionado no es válido.");

        var primaryAccount = await _unitOfWork.SavingsAccounts.GetPrimaryByClientIdAsync(dto.ClientId);
        if (primaryAccount == null || primaryAccount.Status != AccountStatus.Activa)
            throw new InvalidOperationException("El cliente no tiene una cuenta de ahorro principal activa para recibir el desembolso del préstamo.");

        var (averageDebt, hasClients) = await GetAverageDebtAsync();

        var currentLoansDebt = await _unitOfWork.LoanInstallments.GetTotalPendingDebtByClientIdAsync(dto.ClientId);
        var currentCardsDebt = await _unitOfWork.CreditCards.GetTotalActiveDebtByClientIdAsync(dto.ClientId);

        var currentDebt = currentLoansDebt + currentCardsDebt;

        decimal monthlyInterestRate = (dto.AnnualInterestRate / 100m) / 12m;
        decimal baseMonthlyPayment = 0;

        if (monthlyInterestRate > 0)
        {
            var factor = (decimal)Math.Pow((double)(1 + monthlyInterestRate), dto.TermInMonths);
            baseMonthlyPayment = Math.Round(dto.CapitalAmount * (monthlyInterestRate * factor) / (factor - 1), 2);
        }
        else
        {
            baseMonthlyPayment = Math.Round(dto.CapitalAmount / dto.TermInMonths, 2);
        }

        decimal totalToPayNewLoan = 0;
        decimal tempBalance = dto.CapitalAmount;
        for (int i = 1; i <= dto.TermInMonths; i++)
        {
            decimal interestAmount = Math.Round(tempBalance * monthlyInterestRate, 2);
            decimal capitalAmount = Math.Round(baseMonthlyPayment - interestAmount, 2);
            decimal paymentAmount = baseMonthlyPayment;

            if (i == dto.TermInMonths)
            {
                capitalAmount = Math.Round(tempBalance, 2);
                paymentAmount = Math.Round(capitalAmount + interestAmount, 2);
            }

            tempBalance -= capitalAmount;
            totalToPayNewLoan += paymentAmount;
        }

        var projectedDebt = currentDebt + totalToPayNewLoan;

        if (hasClients && !dto.ConfirmHighRisk)
        {
            if (currentDebt > averageDebt)
            {
                throw new HighRiskConflictException(
                    "CurrentHighRisk",
                    currentDebt,
                    projectedDebt,
                    averageDebt,
                    "Este cliente se considera de alto riesgo, ya que su deuda actual supera el promedio del sistema."
                );
            }

            if (projectedDebt > averageDebt)
            {
                throw new HighRiskConflictException(
                    "ProjectedHighRisk",
                    currentDebt,
                    projectedDebt,
                    averageDebt,
                    "Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, ya que su deuda superará el umbral promedio del sistema."
                );
            }
        }

        await _unitOfWork.BeginTransactionAsync();
        Loan loan;
        try
        {
            var loanNumber = await GenerateUniqueLoanNumberAsync();
            loan = new Loan
            {
                ClientId = dto.ClientId,
                LoanNumber = loanNumber,
                ApprovedAmount = dto.CapitalAmount,
                Term = dto.TermInMonths,
                AnnualInterestRate = dto.AnnualInterestRate,
                Status = LoanStatus.Activo,
                CreatedAt = DateTime.UtcNow,
                AdminId = adminId
            };

            await _unitOfWork.Loans.AddAsync(loan);

            await GenerateInstallmentsAsync(loan, monthlyInterestRate, baseMonthlyPayment);
            
            primaryAccount.Balance += dto.CapitalAmount;
            _unitOfWork.SavingsAccounts.Update(primaryAccount);

            var transaction = new Transaction
            {
                SavingsAccountId = primaryAccount.Id,
                Amount = dto.CapitalAmount,
                Type = TransactionType.CRÉDITO,
                Status = TransactionStatus.APROBADA,
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

        var subject = "Préstamo aprobado";
        var body = $"Estimado/a {client.FirstName} {client.LastName},<br><br>Su préstamo número {loan.LoanNumber} por un monto de {dto.CapitalAmount:C} ha sido aprobado y acreditado a su cuenta principal.<br>Plazo: {dto.TermInMonths} meses<br>Tasa de interés anual: {dto.AnnualInterestRate}%<br>Cuota mensual: {baseMonthlyPayment:C}<br><br>Gracias por confiar en nosotros.";
        try
        {
            if (!string.IsNullOrEmpty(client.Email))
            {
                await _emailService.SendAsync(client.Email, subject, body);
            }
        }
        catch (Exception)
        {
        }

        return new LoanCreationResponseDto
        {
            Id = loan.Id,
            LoanNumber = loan.LoanNumber,
            ClientId = loan.ClientId,
            ClientFullName = $"{client.FirstName} {client.LastName}",
            CapitalAmount = dto.CapitalAmount,
            TermInMonths = dto.TermInMonths,
            AnnualInterestRate = dto.AnnualInterestRate,
            MonthlyInstallment = baseMonthlyPayment,
            TotalAmountToPay = totalToPayNewLoan,
            Status = "Activo",
            CreatedAt = loan.CreatedAt
        };
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

                if (i == remainingTerm - 1)
                {
                    capitalAmount = Math.Round(tempBalance, 2);
                    paymentAmount = Math.Round(capitalAmount + interestAmount, 2);
                }

                tempBalance -= capitalAmount;

                inst.Amount = paymentAmount;
                inst.InterestAmount = interestAmount;
                inst.CapitalAmount = capitalAmount;
                inst.PendingBalance = paymentAmount;
                
                _unitOfWork.LoanInstallments.Update(inst);
            }
        }
        else
        {
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

            if (i == loan.Term)
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
                PendingBalance = paymentAmount,
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

public class HighRiskConflictException : Exception
{
    public string RiskType { get; }
    public decimal CurrentDebt { get; }
    public decimal ProjectedDebt { get; }
    public decimal AverageDebt { get; }

    public HighRiskConflictException(string riskType, decimal currentDebt, decimal projectedDebt, decimal averageDebt, string message)
        : base(message)
    {
        RiskType = riskType;
        CurrentDebt = currentDebt;
        ProjectedDebt = projectedDebt;
        AverageDebt = averageDebt;
    }
}
