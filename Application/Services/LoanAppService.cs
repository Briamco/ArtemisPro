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

        if (!string.IsNullOrEmpty(status))
        {
            if (status.ToLower() == "activos")
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

    public async Task<IEnumerable<LoanDto>> GetAllLoansAsync()
    {
        var loans = await _unitOfWork.Loans.Query()
            .Include(l => l.Client)
            .Include(l => l.Installments)
            .ToListAsync();

        return _mapper.Map<IEnumerable<LoanDto>>(loans);
    }

    public async Task<IEnumerable<LoanInstallmentDto>> GetInstallmentsAsync(Guid loanId)
    {
        var installments = await _unitOfWork.LoanInstallments.FindAsync(i => i.LoanId == loanId);
        return _mapper.Map<IEnumerable<LoanInstallmentDto>>(installments.OrderBy(i => i.InstallmentNumber));
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
                Origin = loan.LoanNumber,
                Beneficiary = primaryAccount.AccountNumber
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
        var body = $"""
            <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e1e4e6; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05);">
                <div style="background-color: #1a237e; padding: 20px; text-align: center;">
                    <h1 style="color: white; margin: 0; font-size: 24px; font-weight: bold; letter-spacing: 0.5px;">Artemis Banking Pro</h1>
                </div>
                <div style="padding: 30px; color: #333333; line-height: 1.6;">
                    <h2 style="color: #1a237e; margin-top: 0; font-size: 20px;">¡Hola {client.FirstName} {client.LastName}!</h2>
                    <p>Su préstamo ha sido aprobado correctamente.</p>
                    <div style="background-color: #f4f6f9; border-left: 4px solid #1a237e; padding: 15px 20px; margin: 20px 0; border-radius: 0 6px 6px 0;">
                        <p style="margin: 6px 0;"><strong>Número de préstamo:</strong> {loan.LoanNumber}</p>
                        <p style="margin: 6px 0;"><strong>Monto aprobado:</strong> {dto.CapitalAmount:C}</p>
                        <p style="margin: 6px 0;"><strong>Plazo:</strong> {dto.TermInMonths} meses</p>
                        <p style="margin: 6px 0;"><strong>Tasa de interés anual:</strong> {dto.AnnualInterestRate}%</p>
                        <p style="margin: 6px 0;"><strong>Cuota mensual:</strong> {baseMonthlyPayment:C}</p>
                    </div>
                    <p style="color: #2e7d32; font-weight: 600;">El monto aprobado ha sido depositado en su cuenta de ahorro principal.</p>
                </div>
                <div style="background-color: #f8f9fa; padding: 15px 30px; text-align: center; font-size: 12px; color: #666666; border-top: 1px solid #e1e4e6;">
                    Este es un correo automático, por favor no responda a este mensaje.<br/>
                    &copy; {DateTime.UtcNow.Year} Artemis Banking Pro. Todos los derechos reservados.
                </div>
            </div>
            """;
        try
        {
            if (!string.IsNullOrEmpty(client.Email))
            {
                await _emailService.SendAsync(client.Email, subject, body);
            }
        }
        catch (Exception)
        {
            // El fallo de envío de email no es crítico — el préstamo ya fue creado exitosamente.
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
            var nextInstallment = futureInstallments.FirstOrDefault();
            decimal nextInstallmentAmount = nextInstallment?.Amount ?? 0m;
            string nextDueDateStr = nextInstallment?.DueDate.ToString("dd/MM/yyyy") ?? "N/A";

            var subject = "Actualización de tasa de interés de préstamo";
            var body = $"""
                <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e1e4e6; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05);">
                    <div style="background-color: #1a237e; padding: 20px; text-align: center;">
                        <h1 style="color: white; margin: 0; font-size: 24px; font-weight: bold; letter-spacing: 0.5px;">Artemis Banking Pro</h1>
                    </div>
                    <div style="padding: 30px; color: #333333; line-height: 1.6;">
                        <h2 style="color: #1a237e; margin-top: 0; font-size: 20px;">¡Hola {client.FirstName} {client.LastName}!</h2>
                        <p>La tasa de interés de su préstamo <strong>{loan.LoanNumber}</strong> ha sido actualizada.</p>
                        <div style="background-color: #f4f6f9; border-left: 4px solid #1a237e; padding: 15px 20px; margin: 20px 0; border-radius: 0 6px 6px 0;">
                            <p style="margin: 6px 0;"><strong>Número de préstamo:</strong> {loan.LoanNumber}</p>
                            <p style="margin: 6px 0;"><strong>Nueva tasa de interés anual:</strong> {dto.AnnualInterestRate}%</p>
                            <p style="margin: 6px 0;"><strong>Nuevo valor de la próxima cuota:</strong> {nextInstallmentAmount:C}</p>
                            <p style="margin: 6px 0;"><strong>Fecha de vencimiento de la próxima cuota:</strong> {nextDueDateStr}</p>
                        </div>
                        <p style="color: #666666; font-size: 14px;">Esta modificación aplica únicamente a las cuotas futuras pendientes.</p>
                    </div>
                    <div style="background-color: #f8f9fa; padding: 15px 30px; text-align: center; font-size: 12px; color: #666666; border-top: 1px solid #e1e4e6;">
                        Este es un correo automático, por favor no responda a este mensaje.<br/>
                        &copy; {DateTime.UtcNow.Year} Artemis Banking Pro. Todos los derechos reservados.
                    </div>
                </div>
                """;
            try
            {
                await _emailService.SendAsync(client.Email, subject, body);
            }
            catch (Exception)
            {
                // El fallo de envío de email no es crítico — la tasa ya fue actualizada exitosamente.
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

    public async Task<IEnumerable<LoanDto>> GetClientLoansAsync(Guid clientId)
    {
        var loansQuery = _unitOfWork.Loans.Query();
        loansQuery = loansQuery.Include(l => l.Client).Include(l => l.Installments);
        var loans = await loansQuery.ToListAsync();
        var clientLoans = loans.Where(l => l.ClientId == clientId);
        return _mapper.Map<IEnumerable<LoanDto>>(clientLoans);
    }
}
