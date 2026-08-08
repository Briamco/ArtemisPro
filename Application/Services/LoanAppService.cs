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

namespace Application.Services;

public class LoanAppService : ILoanAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public LoanAppService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<LoanDto>> GetLoansAsync(string? status = null, string? cedula = null)
    {
        var loans = await _unitOfWork.Loans.GetAllAsync();
        
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<LoanStatus>(status, out var loanStatus))
        {
            loans = loans.Where(l => l.Status == loanStatus);
        }

        if (!string.IsNullOrEmpty(cedula))
        {
            var user = (await _unitOfWork.Users.FindAsync(u => u.Cedula == cedula)).FirstOrDefault();
            if (user != null)
            {
                loans = loans.Where(l => l.ClientId == user.Id);
            }
        }

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

    public async Task<(bool Success, string? Error, string? WarningMessage, bool IsHighRisk)> CreateLoanAsync(CreateLoanDto dto)
    {
        var client = await _unitOfWork.Users.GetByIdAsync(dto.ClientId);
        if (client == null)
            return (false, "Cliente no encontrado.", null, false);

        // 1. Calculate Average Debt of the System
        var (averageDebt, hasClients) = await GetAverageDebtAsync();

        // 2. Calculate Current Debt of the Client
        var clientLoans = await _unitOfWork.Loans.GetByClientIdAsync(dto.ClientId);
        var activeLoansDebt = clientLoans.Where(l => l.Status == LoanStatus.Activo)
            .Sum(l => l.Installments?.Where(i => i.PaymentStatus == PaymentStatus.Pendiente).Sum(i => i.Amount) ?? 0); 
            // Better to fetch installments if they are not loaded, or simply use a query. 
            // Let's assume we can query installments directly to be safe.
        
        var allClientInstallments = await _unitOfWork.LoanInstallments.FindAsync(i => clientLoans.Select(cl => cl.Id).Contains(i.LoanId) && i.PaymentStatus == PaymentStatus.Pendiente);
        var currentLoansDebt = allClientInstallments.Sum(i => i.Amount);

        var clientCards = await _unitOfWork.CreditCards.GetByClientIdAsync(dto.ClientId);
        var currentCardsDebt = clientCards.Where(c => c.Status == CardStatus.Activa).Sum(c => c.Debt);

        var currentDebt = currentLoansDebt + currentCardsDebt;

        // Check if currently above average debt
        if (hasClients && currentDebt > averageDebt)
        {
            return (false, null, "Este cliente se considera de alto riesgo, ya que su deuda actual supera el promedio del sistema.", true);
        }

        // 3. Calculate Projected Debt
        decimal monthlyInterestRate = (dto.AnnualInterestRate / 100m) / 12m;
        decimal totalToPayNewLoan = 0;

        if (monthlyInterestRate > 0)
        {
            var factor = (decimal)Math.Pow((double)(1 + monthlyInterestRate), dto.Term);
            var monthlyPayment = dto.ApprovedAmount * (monthlyInterestRate * factor) / (factor - 1);
            totalToPayNewLoan = monthlyPayment * dto.Term;
        }
        else
        {
            totalToPayNewLoan = dto.ApprovedAmount;
        }

        var projectedDebt = currentDebt + totalToPayNewLoan;

        // Check if projected debt above average debt
        if (hasClients && projectedDebt > averageDebt)
        {
            return (false, null, "Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, ya que su deuda superará el umbral promedio del sistema.", true);
        }

        // Proceed to create loan
        var loan = new Loan
        {
            ClientId = dto.ClientId,
            LoanNumber = GenerateLoanNumber(),
            ApprovedAmount = dto.ApprovedAmount,
            Term = dto.Term,
            AnnualInterestRate = dto.AnnualInterestRate,
            Status = LoanStatus.Activo,
            CreatedAt = DateTime.UtcNow
            // AdminId should be set here, but we lack it in DTO. Assuming it's set in controller or default empty guid
        };

        await _unitOfWork.Loans.AddAsync(loan);
        await _unitOfWork.SaveChangesAsync();

        // Generate Installments
        await GenerateInstallmentsAsync(loan, monthlyInterestRate, totalToPayNewLoan / dto.Term);
        await _unitOfWork.SaveChangesAsync();

        return (true, null, null, false);
    }

    public async Task<(bool Success, string? Error)> UpdateLoanRateAsync(Guid id, UpdateLoanRateDto dto)
    {
        var loan = await _unitOfWork.Loans.GetByIdAsync(id);
        if (loan == null) return (false, "Préstamo no encontrado.");

        loan.AnnualInterestRate = dto.AnnualInterestRate;
        _unitOfWork.Loans.Update(loan);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(decimal AverageDebt, bool HasClients)> GetAverageDebtAsync()
    {
        var allLoans = await _unitOfWork.Loans.GetAllAsync();
        var allCards = await _unitOfWork.CreditCards.GetAllAsync();
        
        var activeLoans = allLoans.Where(l => l.Status == LoanStatus.Activo).ToList();
        var activeCards = allCards.Where(c => c.Status == CardStatus.Activa).ToList();

        var clientsWithDebt = activeLoans.Select(l => l.ClientId)
            .Union(activeCards.Select(c => c.ClientId))
            .Distinct()
            .ToList();

        if (!clientsWithDebt.Any())
        {
            return (0, false);
        }

        decimal totalDebt = 0;

        foreach (var clientId in clientsWithDebt)
        {
            var clientLoans = activeLoans.Where(l => l.ClientId == clientId);
            var clientLoanIds = clientLoans.Select(l => l.Id).ToList();
            
            var installments = await _unitOfWork.LoanInstallments.FindAsync(i => clientLoanIds.Contains(i.LoanId) && i.PaymentStatus == PaymentStatus.Pendiente);
            totalDebt += installments.Sum(i => i.Amount);

            totalDebt += activeCards.Where(c => c.ClientId == clientId).Sum(c => c.Debt);
        }

        return (totalDebt / clientsWithDebt.Count, true);
    }

    private string GenerateLoanNumber()
    {
        return $"LN-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";
    }

    private async Task GenerateInstallmentsAsync(Loan loan, decimal monthlyInterestRate, decimal monthlyPayment)
    {
        decimal pendingBalance = loan.ApprovedAmount;
        var installments = new List<LoanInstallment>();

        for (int i = 1; i <= loan.Term; i++)
        {
            decimal interestAmount = pendingBalance * monthlyInterestRate;
            decimal capitalAmount = monthlyPayment - interestAmount;

            if (i == loan.Term) // Adjust last payment to fix decimals
            {
                capitalAmount = pendingBalance;
                monthlyPayment = capitalAmount + interestAmount;
            }

            pendingBalance -= capitalAmount;

            var installment = new LoanInstallment
            {
                LoanId = loan.Id,
                InstallmentNumber = i,
                DueDate = loan.CreatedAt.AddMonths(i),
                Amount = monthlyPayment,
                InterestAmount = interestAmount,
                CapitalAmount = capitalAmount,
                PendingBalance = Math.Max(0, pendingBalance), // Prevent negative balance
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
