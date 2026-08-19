using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.SavingsAccounts.Commands;

public record CreateSavingsAccountCommand(CreateSavingsAccountApiDto Dto, Guid? AdminId) : IRequest<(bool Success, string? ErrorCode, string? ErrorMessage, SavingsAccountApiDto? Account)>;

public class CreateSavingsAccountCommandValidator : AbstractValidator<CreateSavingsAccountCommand>
{
    public CreateSavingsAccountCommandValidator()
    {
        RuleFor(x => x.Dto.ClientId).NotEmpty().WithMessage("El identificador del cliente es requerido.");
        RuleFor(x => x.Dto.InitialBalance).GreaterThanOrEqualTo(0).WithMessage("El balance inicial no puede ser negativo.");
    }
}

public class CreateSavingsAccountCommandHandler : IRequestHandler<CreateSavingsAccountCommand, (bool Success, string? ErrorCode, string? ErrorMessage, SavingsAccountApiDto? Account)>
{
    private readonly ISavingsAccountAppService _savingsAccountAppService;

    public CreateSavingsAccountCommandHandler(ISavingsAccountAppService savingsAccountAppService)
    {
        _savingsAccountAppService = savingsAccountAppService;
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, SavingsAccountApiDto? Account)> Handle(CreateSavingsAccountCommand request, CancellationToken cancellationToken)
    {
        return await _savingsAccountAppService.CreateSavingsAccountApiAsync(request.Dto, request.AdminId);
    }
}

public record CancelSavingsAccountCommand(string AccountNumber) : IRequest<(bool Success, string? ErrorCode, string? ErrorMessage)>;

public class CancelSavingsAccountCommandValidator : AbstractValidator<CancelSavingsAccountCommand>
{
    public CancelSavingsAccountCommandValidator()
    {
        RuleFor(x => x.AccountNumber).NotEmpty().WithMessage("El número de cuenta es requerido.");
    }
}

public class CancelSavingsAccountCommandHandler : IRequestHandler<CancelSavingsAccountCommand, (bool Success, string? ErrorCode, string? ErrorMessage)>
{
    private readonly ISavingsAccountAppService _savingsAccountAppService;

    public CancelSavingsAccountCommandHandler(ISavingsAccountAppService savingsAccountAppService)
    {
        _savingsAccountAppService = savingsAccountAppService;
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> Handle(CancelSavingsAccountCommand request, CancellationToken cancellationToken)
    {
        return await _savingsAccountAppService.CancelSavingsAccountApiAsync(request.AccountNumber);
    }
}
