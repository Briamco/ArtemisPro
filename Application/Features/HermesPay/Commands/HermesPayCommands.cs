using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.HermesPay.Commands;

public record ProcessPaymentCommand(Guid CommerceId, ProcessPaymentDto Dto) : IRequest<(bool Success, string? ErrorCode, string? ErrorMessage)>;

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.Dto.CardNumber).NotEmpty().WithMessage("El número de tarjeta es requerido.")
            .Length(16).WithMessage("El número de tarjeta debe contener exactamente 16 dígitos.");
        RuleFor(x => x.Dto.MonthExpirationCard).NotEmpty().WithMessage("El mes de expiración es requerido.");
        RuleFor(x => x.Dto.YearExpirationCard).NotEmpty().WithMessage("El año de expiración es requerido.");
        RuleFor(x => x.Dto.Cvc).NotEmpty().WithMessage("El CVC es requerido.")
            .Length(3).WithMessage("El CVC debe contener exactamente 3 dígitos.");
        RuleFor(x => x.Dto.TransactionAmount).GreaterThan(0).WithMessage("El monto de la transacción debe ser mayor que cero.");
    }
}

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, (bool Success, string? ErrorCode, string? ErrorMessage)>
{
    private readonly IHermesPayAppService _hermesPayAppService;

    public ProcessPaymentCommandHandler(IHermesPayAppService hermesPayAppService)
    {
        _hermesPayAppService = hermesPayAppService;
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        return await _hermesPayAppService.ProcessPaymentAsync(request.CommerceId, request.Dto);
    }
}
