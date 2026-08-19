using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.CreditCards.Commands;

public record AssignCreditCardCommand(AssignCreditCardDto Dto) : IRequest<(bool Success, string? Error, CreditCardDto? Card)>;

public class AssignCreditCardCommandValidator : AbstractValidator<AssignCreditCardCommand>
{
    public AssignCreditCardCommandValidator()
    {
        RuleFor(x => x.Dto.ClientId).NotEmpty().WithMessage("El identificador del cliente es requerido.");
        RuleFor(x => x.Dto.Limit).GreaterThan(0).WithMessage("El límite de crédito debe ser mayor que cero.");
    }
}

public class AssignCreditCardCommandHandler : IRequestHandler<AssignCreditCardCommand, (bool Success, string? Error, CreditCardDto? Card)>
{
    private readonly ICreditCardAppService _creditCardAppService;

    public AssignCreditCardCommandHandler(ICreditCardAppService creditCardAppService)
    {
        _creditCardAppService = creditCardAppService;
    }

    public async Task<(bool Success, string? Error, CreditCardDto? Card)> Handle(AssignCreditCardCommand request, CancellationToken cancellationToken)
    {
        return await _creditCardAppService.AssignCreditCardAsync(request.Dto);
    }
}

public record UpdateCreditCardLimitCommand(Guid Id, UpdateCreditCardLimitDto Dto) : IRequest<(bool Success, string? Error)>;

public class UpdateCreditCardLimitCommandValidator : AbstractValidator<UpdateCreditCardLimitCommand>
{
    public UpdateCreditCardLimitCommandValidator()
    {
        RuleFor(x => x.Dto.NewLimit).GreaterThan(0).WithMessage("El nuevo límite debe ser mayor que cero.");
    }
}

public class UpdateCreditCardLimitCommandHandler : IRequestHandler<UpdateCreditCardLimitCommand, (bool Success, string? Error)>
{
    private readonly ICreditCardAppService _creditCardAppService;

    public UpdateCreditCardLimitCommandHandler(ICreditCardAppService creditCardAppService)
    {
        _creditCardAppService = creditCardAppService;
    }

    public async Task<(bool Success, string? Error)> Handle(UpdateCreditCardLimitCommand request, CancellationToken cancellationToken)
    {
        return await _creditCardAppService.UpdateCreditCardLimitAsync(request.Id, request.Dto);
    }
}

public record CancelCreditCardCommand(Guid Id) : IRequest<(bool Success, string? Error)>;

public class CancelCreditCardCommandHandler : IRequestHandler<CancelCreditCardCommand, (bool Success, string? Error)>
{
    private readonly ICreditCardAppService _creditCardAppService;

    public CancelCreditCardCommandHandler(ICreditCardAppService creditCardAppService)
    {
        _creditCardAppService = creditCardAppService;
    }

    public async Task<(bool Success, string? Error)> Handle(CancelCreditCardCommand request, CancellationToken cancellationToken)
    {
        return await _creditCardAppService.CancelCreditCardAsync(request.Id);
    }
}
