using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Commerce.Queries;

public record GetCommercesPagedQuery(int Page, int PageSize, string? Status) : IRequest<PagedResultDto<CommerceDto>>;

public class GetCommercesPagedQueryValidator : AbstractValidator<GetCommercesPagedQuery>
{
    public GetCommercesPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");
        RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
            .LessThanOrEqualTo(20).WithMessage("El valor máximo permitido para pageSize debe ser 20.");
    }
}

public class GetCommercesPagedQueryHandler : IRequestHandler<GetCommercesPagedQuery, PagedResultDto<CommerceDto>>
{
    private readonly ICommerceAppService _commerceAppService;

    public GetCommercesPagedQueryHandler(ICommerceAppService commerceAppService)
    {
        _commerceAppService = commerceAppService;
    }

    public async Task<PagedResultDto<CommerceDto>> Handle(GetCommercesPagedQuery request, CancellationToken cancellationToken)
    {
        return await _commerceAppService.GetCommercesPagedAsync(request.Page, request.PageSize, request.Status);
    }
}

public record GetCommerceByIdQuery(Guid Id) : IRequest<CommerceDetailDto?>;

public class GetCommerceByIdQueryHandler : IRequestHandler<GetCommerceByIdQuery, CommerceDetailDto?>
{
    private readonly ICommerceAppService _commerceAppService;

    public GetCommerceByIdQueryHandler(ICommerceAppService commerceAppService)
    {
        _commerceAppService = commerceAppService;
    }

    public async Task<CommerceDetailDto?> Handle(GetCommerceByIdQuery request, CancellationToken cancellationToken)
    {
        return await _commerceAppService.GetCommerceByIdAsync(request.Id);
    }
}
