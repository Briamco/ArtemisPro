using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.HermesPay.Queries;

public record GetCommerceTransactionsQuery(Guid CommerceId, int Page, int PageSize) : IRequest<(bool Success, string? ErrorCode, string? ErrorMessage, CommerceTransactionsResponseDto? Result)>;

public class GetCommerceTransactionsQueryValidator : AbstractValidator<GetCommerceTransactionsQuery>
{
    public GetCommerceTransactionsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");
        RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
            .LessThanOrEqualTo(20).WithMessage("El valor máximo permitido para pageSize debe ser 20.");
    }
}

public class GetCommerceTransactionsQueryHandler : IRequestHandler<GetCommerceTransactionsQuery, (bool Success, string? ErrorCode, string? ErrorMessage, CommerceTransactionsResponseDto? Result)>
{
    private readonly IHermesPayAppService _hermesPayAppService;

    public GetCommerceTransactionsQueryHandler(IHermesPayAppService hermesPayAppService)
    {
        _hermesPayAppService = hermesPayAppService;
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, CommerceTransactionsResponseDto? Result)> Handle(GetCommerceTransactionsQuery request, CancellationToken cancellationToken)
    {
        return await _hermesPayAppService.GetCommerceTransactionsAsync(request.CommerceId, request.Page, request.PageSize);
    }
}
