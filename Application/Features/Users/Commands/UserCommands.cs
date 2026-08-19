using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Identity;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Users.Commands;

public record CreateUserCommand(CreateUserApiDto Dto) : IRequest<(bool Success, string? ErrorCode, string? ErrorMessage, CreatedUserResponseApiDto? User)>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Dto.FirstName).NotEmpty().WithMessage("El nombre es requerido.");
        RuleFor(x => x.Dto.LastName).NotEmpty().WithMessage("El apellido es requerido.");
        RuleFor(x => x.Dto.Identification).NotEmpty().WithMessage("La cédula es requerida.");
        RuleFor(x => x.Dto.Email).NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El formato de correo no es válido.");
        RuleFor(x => x.Dto.UserName).NotEmpty().WithMessage("El nombre de usuario es requerido.");
        RuleFor(x => x.Dto.Password).NotEmpty().WithMessage("La contraseña es requerida.");
        RuleFor(x => x.Dto.ConfirmPassword).NotEmpty().WithMessage("La confirmación de contraseña es requerida.")
            .Equal(x => x.Dto.Password).WithMessage("La contraseña y la confirmación deben coincidir.");
        RuleFor(x => x.Dto.Role).NotEmpty().WithMessage("El rol es requerido.");
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, (bool Success, string? ErrorCode, string? ErrorMessage, CreatedUserResponseApiDto? User)>
{
    private readonly IUserAppService _userAppService;

    public CreateUserCommandHandler(IUserAppService userAppService)
    {
        _userAppService = userAppService;
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, CreatedUserResponseApiDto? User)> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        return await _userAppService.CreateUserApiAsync(request.Dto);
    }
}

public record CreateCommerceUserCommand(Guid CommerceId, CreateCommerceUserApiDto Dto) : IRequest<(bool Success, string? ErrorCode, string? ErrorMessage, CreatedUserResponseApiDto? User)>;

public class CreateCommerceUserCommandValidator : AbstractValidator<CreateCommerceUserCommand>
{
    public CreateCommerceUserCommandValidator()
    {
        RuleFor(x => x.Dto.FirstName).NotEmpty().WithMessage("El nombre es requerido.");
        RuleFor(x => x.Dto.LastName).NotEmpty().WithMessage("El apellido es requerido.");
        RuleFor(x => x.Dto.Identification).NotEmpty().WithMessage("La cédula o identificador es requerida.");
        RuleFor(x => x.Dto.Email).NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El formato de correo no es válido.");
        RuleFor(x => x.Dto.UserName).NotEmpty().WithMessage("El nombre de usuario es requerido.");
        RuleFor(x => x.Dto.Password).NotEmpty().WithMessage("La contraseña es requerida.");
        RuleFor(x => x.Dto.ConfirmPassword).NotEmpty().WithMessage("La confirmación de contraseña es requerida.")
            .Equal(x => x.Dto.Password).WithMessage("La contraseña y la confirmación deben coincidir.");
    }
}

public class CreateCommerceUserCommandHandler : IRequestHandler<CreateCommerceUserCommand, (bool Success, string? ErrorCode, string? ErrorMessage, CreatedUserResponseApiDto? User)>
{
    private readonly IUserAppService _userAppService;

    public CreateCommerceUserCommandHandler(IUserAppService userAppService)
    {
        _userAppService = userAppService;
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, CreatedUserResponseApiDto? User)> Handle(CreateCommerceUserCommand request, CancellationToken cancellationToken)
    {
        return await _userAppService.CreateCommerceUserApiAsync(request.CommerceId, request.Dto);
    }
}

public record UpdateUserCommand(Guid Id, UpdateUserApiDto Dto) : IRequest<(bool Success, string? ErrorCode, string? ErrorMessage)>;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Dto.FirstName).NotEmpty().WithMessage("El nombre es requerido.");
        RuleFor(x => x.Dto.LastName).NotEmpty().WithMessage("El apellido es requerido.");
        RuleFor(x => x.Dto.Identification).NotEmpty().WithMessage("La cédula es requerida.");
        RuleFor(x => x.Dto.Email).NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El formato de correo no es válido.");
        RuleFor(x => x.Dto.UserName).NotEmpty().WithMessage("El nombre de usuario es requerido.");
    }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, (bool Success, string? ErrorCode, string? ErrorMessage)>
{
    private readonly IUserAppService _userAppService;

    public UpdateUserCommandHandler(IUserAppService userAppService)
    {
        _userAppService = userAppService;
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        return await _userAppService.UpdateUserApiAsync(request.Id, request.Dto);
    }
}

public record UpdateUserStatusCommand(Guid Id, bool Status, Guid AdminId) : IRequest<(bool Success, string? ErrorCode, string? ErrorMessage)>;

public class UpdateUserStatusCommandHandler : IRequestHandler<UpdateUserStatusCommand, (bool Success, string? ErrorCode, string? ErrorMessage)>
{
    private readonly IUserAppService _userAppService;

    public UpdateUserStatusCommandHandler(IUserAppService userAppService)
    {
        _userAppService = userAppService;
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
    {
        return await _userAppService.UpdateUserStatusApiAsync(request.Id, request.Status, request.AdminId);
    }
}
