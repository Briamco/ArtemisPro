using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.DTOs.Identity;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Users.Queries;

public record GetUsersPagedQuery(int Page, int PageSize, string? Role) : IRequest<PagedResultDto<UserApiDto>>;

public class GetUsersPagedQueryValidator : AbstractValidator<GetUsersPagedQuery>
{
    public GetUsersPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");
        RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
            .LessThanOrEqualTo(20).WithMessage("El valor máximo permitido para pageSize debe ser 20.");
    }
}

public class GetUsersPagedQueryHandler : IRequestHandler<GetUsersPagedQuery, PagedResultDto<UserApiDto>>
{
    private readonly IUserAppService _userAppService;

    public GetUsersPagedQueryHandler(IUserAppService userAppService)
    {
        _userAppService = userAppService;
    }

    public async Task<PagedResultDto<UserApiDto>> Handle(GetUsersPagedQuery request, CancellationToken cancellationToken)
    {
        return await _userAppService.GetUsersPagedApiAsync(request.Page, request.PageSize, request.Role);
    }
}

public record GetCommerceUsersPagedQuery(int Page, int PageSize) : IRequest<PagedResultDto<CommerceUserApiDto>>;

public class GetCommerceUsersPagedQueryValidator : AbstractValidator<GetCommerceUsersPagedQuery>
{
    public GetCommerceUsersPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");
        RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
            .LessThanOrEqualTo(20).WithMessage("El valor máximo permitido para pageSize debe ser 20.");
    }
}

public class GetCommerceUsersPagedQueryHandler : IRequestHandler<GetCommerceUsersPagedQuery, PagedResultDto<CommerceUserApiDto>>
{
    private readonly IUserAppService _userAppService;

    public GetCommerceUsersPagedQueryHandler(IUserAppService userAppService)
    {
        _userAppService = userAppService;
    }

    public async Task<PagedResultDto<CommerceUserApiDto>> Handle(GetCommerceUsersPagedQuery request, CancellationToken cancellationToken)
    {
        return await _userAppService.GetCommerceUsersPagedApiAsync(request.Page, request.PageSize);
    }
}

public record GetUserDetailQuery(Guid Id) : IRequest<UserDetailApiDto?>;

public class GetUserDetailQueryHandler : IRequestHandler<GetUserDetailQuery, UserDetailApiDto?>
{
    private readonly IUserAppService _userAppService;

    public GetUserDetailQueryHandler(IUserAppService userAppService)
    {
        _userAppService = userAppService;
    }

    public async Task<UserDetailApiDto?> Handle(GetUserDetailQuery request, CancellationToken cancellationToken)
    {
        return await _userAppService.GetUserDetailApiAsync(request.Id);
    }
}
