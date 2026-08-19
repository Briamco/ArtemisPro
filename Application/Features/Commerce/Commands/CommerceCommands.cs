using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Commerce.Commands;

public record CreateCommerceCommand(CreateCommerceDto Dto, Guid? AdminId) : IRequest<(bool Success, string? ErrorCode, string? ErrorMessage, CommerceDto? Commerce)>;

public class CreateCommerceCommandValidator : AbstractValidator<CreateCommerceCommand>
{
    public CreateCommerceCommandValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().WithMessage("El nombre del comercio es obligatorio.");
        RuleFor(x => x.Dto.Email).NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El correo electrónico debe tener un formato válido.");
        RuleFor(x => x.Dto.PhoneNumber).NotEmpty().WithMessage("El teléfono es obligatorio.");
        RuleFor(x => x.Dto.RNC).NotEmpty().WithMessage("El RNC es obligatorio.");
    }
}

public class CreateCommerceCommandHandler : IRequestHandler<CreateCommerceCommand, (bool Success, string? ErrorCode, string? ErrorMessage, CommerceDto? Commerce)>
{
    private readonly ICommerceAppService _commerceAppService;

    public CreateCommerceCommandHandler(ICommerceAppService commerceAppService)
    {
        _commerceAppService = commerceAppService;
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, CommerceDto? Commerce)> Handle(CreateCommerceCommand request, CancellationToken cancellationToken)
    {
        return await _commerceAppService.CreateCommerceAsync(request.Dto, request.AdminId);
    }
}

public record UpdateCommerceCommand(Guid Id, UpdateCommerceDto Dto) : IRequest<(bool Success, string? ErrorCode, string? ErrorMessage)>;

public class UpdateCommerceCommandValidator : AbstractValidator<UpdateCommerceCommand>
{
    public UpdateCommerceCommandValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().WithMessage("El nombre del comercio es obligatorio.");
        RuleFor(x => x.Dto.Email).NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El correo electrónico debe tener un formato válido.");
        RuleFor(x => x.Dto.PhoneNumber).NotEmpty().WithMessage("El teléfono es obligatorio.");
        RuleFor(x => x.Dto.RNC).NotEmpty().WithMessage("El RNC es obligatorio.");
    }
}

public class UpdateCommerceCommandHandler : IRequestHandler<UpdateCommerceCommand, (bool Success, string? ErrorCode, string? ErrorMessage)>
{
    private readonly ICommerceAppService _commerceAppService;

    public UpdateCommerceCommandHandler(ICommerceAppService commerceAppService)
    {
        _commerceAppService = commerceAppService;
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> Handle(UpdateCommerceCommand request, CancellationToken cancellationToken)
    {
        return await _commerceAppService.UpdateCommerceAsync(request.Id, request.Dto);
    }
}

public record UpdateCommerceStatusCommand(Guid Id, bool Status) : IRequest<(bool Success, string? ErrorCode, string? ErrorMessage)>;

public class UpdateCommerceStatusCommandHandler : IRequestHandler<UpdateCommerceStatusCommand, (bool Success, string? ErrorCode, string? ErrorMessage)>
{
    private readonly ICommerceAppService _commerceAppService;

    public UpdateCommerceStatusCommandHandler(ICommerceAppService commerceAppService)
    {
        _commerceAppService = commerceAppService;
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> Handle(UpdateCommerceStatusCommand request, CancellationToken cancellationToken)
    {
        return await _commerceAppService.UpdateCommerceStatusAsync(request.Id, request.Status);
    }
}
