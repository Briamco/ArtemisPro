using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Loans.Commands;

public record CreateLoanCommand(CreateLoanDto Dto, Guid AdminId) : IRequest<LoanCreationResponseDto>;

public class CreateLoanCommandValidator : AbstractValidator<CreateLoanCommand>
{
    public CreateLoanCommandValidator()
    {
        RuleFor(x => x.Dto.ClientId).NotEmpty().WithMessage("El identificador del cliente es requerido.");
        RuleFor(x => x.Dto.CapitalAmount).GreaterThan(0).WithMessage("El monto del préstamo debe ser mayor que cero.");
        RuleFor(x => x.Dto.AnnualInterestRate).GreaterThanOrEqualTo(0).WithMessage("La tasa de interés anual no puede ser negativa.");
        RuleFor(x => x.Dto.TermInMonths).Must(term => new[] { 6, 12, 18, 24, 30, 36, 42, 48, 54, 60 }.Contains(term))
            .WithMessage("El plazo debe ser uno de los valores permitidos (6, 12, 18, 24, 30, 36, 42, 48, 54, 60).");
    }
}

public class CreateLoanCommandHandler : IRequestHandler<CreateLoanCommand, LoanCreationResponseDto>
{
    private readonly ILoanAppService _loanAppService;

    public CreateLoanCommandHandler(ILoanAppService loanAppService)
    {
        _loanAppService = loanAppService;
    }

    public async Task<LoanCreationResponseDto> Handle(CreateLoanCommand request, CancellationToken cancellationToken)
    {
        return await _loanAppService.CreateLoanAsync(request.Dto, request.AdminId);
    }
}

public record UpdateLoanRateCommand(Guid Id, UpdateLoanRateDto Dto) : IRequest<(bool Success, string? Error)>;

public class UpdateLoanRateCommandValidator : AbstractValidator<UpdateLoanRateCommand>
{
    public UpdateLoanRateCommandValidator()
    {
        RuleFor(x => x.Dto.AnnualInterestRate).GreaterThanOrEqualTo(0).WithMessage("La tasa de interés anual no puede ser negativa.");
    }
}

public class UpdateLoanRateCommandHandler : IRequestHandler<UpdateLoanRateCommand, (bool Success, string? Error)>
{
    private readonly ILoanAppService _loanAppService;

    public UpdateLoanRateCommandHandler(ILoanAppService loanAppService)
    {
        _loanAppService = loanAppService;
    }

    public async Task<(bool Success, string? Error)> Handle(UpdateLoanRateCommand request, CancellationToken cancellationToken)
    {
        return await _loanAppService.UpdateLoanRateAsync(request.Id, request.Dto);
    }
}
