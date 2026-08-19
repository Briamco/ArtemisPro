using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Identity;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.Account.Commands;

public record LoginCommand(LoginDto Dto) : IRequest<ApiLoginResult>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Dto.UserName).NotEmpty().WithMessage("El nombre de usuario es requerido.");
        RuleFor(x => x.Dto.Password).NotEmpty().WithMessage("La contraseña es requerida.");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiLoginResult>
{
    private readonly IAuthAppService _authService;

    public LoginCommandHandler(IAuthAppService authService)
    {
        _authService = authService;
    }

    public async Task<ApiLoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return await _authService.ApiLoginAsync(request.Dto);
    }
}

public record ConfirmAccountCommand(string Token) : IRequest<IdentityResult>;

public class ConfirmAccountCommandValidator : AbstractValidator<ConfirmAccountCommand>
{
    public ConfirmAccountCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("El token es requerido.");
    }
}

public class ConfirmAccountCommandHandler : IRequestHandler<ConfirmAccountCommand, IdentityResult>
{
    private readonly IAuthAppService _authService;

    public ConfirmAccountCommandHandler(IAuthAppService authService)
    {
        _authService = authService;
    }

    public async Task<IdentityResult> Handle(ConfirmAccountCommand request, CancellationToken cancellationToken)
    {
        return await _authService.ConfirmAccountByTokenAsync(request.Token);
    }
}

public record GetResetTokenCommand(string UserName) : IRequest<(bool Succeeded, string? ErrorMessage)>;

public class GetResetTokenCommandValidator : AbstractValidator<GetResetTokenCommand>
{
    public GetResetTokenCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithMessage("El nombre de usuario es requerido.");
    }
}

public class GetResetTokenCommandHandler : IRequestHandler<GetResetTokenCommand, (bool Succeeded, string? ErrorMessage)>
{
    private readonly IAuthAppService _authService;

    public GetResetTokenCommandHandler(IAuthAppService authService)
    {
        _authService = authService;
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> Handle(GetResetTokenCommand request, CancellationToken cancellationToken)
    {
        return await _authService.GetResetTokenApiAsync(request.UserName);
    }
}

public record ResetPasswordCommand(string UserId, string Token, string Password, string ConfirmPassword) : IRequest<IdentityResult>;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("El identificador del usuario es requerido.");
        RuleFor(x => x.Token).NotEmpty().WithMessage("El token es requerido.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("La contraseña es requerida.");
        RuleFor(x => x.ConfirmPassword).NotEmpty().WithMessage("La confirmación de contraseña es requerida.")
            .Equal(x => x.Password).WithMessage("La contraseña y la confirmación de contraseña deben coincidir.");
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, IdentityResult>
{
    private readonly IAuthAppService _authService;

    public ResetPasswordCommandHandler(IAuthAppService authService)
    {
        _authService = authService;
    }

    public async Task<IdentityResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        return await _authService.ResetPasswordApiAsync(request.UserId, request.Token, request.Password, request.ConfirmPassword);
    }
}
