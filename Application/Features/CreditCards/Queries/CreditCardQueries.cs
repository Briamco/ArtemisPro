using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.CreditCards.Queries;

public record GetCreditCardsPagedQuery(int Page, int PageSize, string? Status, string? Identification) : IRequest<PagedResultDto<CreditCardDto>>;

public class GetCreditCardsPagedQueryValidator : AbstractValidator<GetCreditCardsPagedQuery>
{
    public GetCreditCardsPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");
        RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
            .LessThanOrEqualTo(20).WithMessage("El valor máximo permitido para pageSize debe ser 20.");
    }
}

public class GetCreditCardsPagedQueryHandler : IRequestHandler<GetCreditCardsPagedQuery, PagedResultDto<CreditCardDto>>
{
    private readonly ICreditCardAppService _creditCardAppService;

    public GetCreditCardsPagedQueryHandler(ICreditCardAppService creditCardAppService)
    {
        _creditCardAppService = creditCardAppService;
    }

    public async Task<PagedResultDto<CreditCardDto>> Handle(GetCreditCardsPagedQuery request, CancellationToken cancellationToken)
    {
        return await _creditCardAppService.GetCreditCardsPagedAsync(request.Page, request.PageSize, request.Status, request.Identification);
    }
}

public record GetCreditCardDetailQuery(Guid Id) : IRequest<CreditCardDetailDto?>;

public class GetCreditCardDetailQueryHandler : IRequestHandler<GetCreditCardDetailQuery, CreditCardDetailDto?>
{
    private readonly ICreditCardAppService _creditCardAppService;

    public GetCreditCardDetailQueryHandler(ICreditCardAppService creditCardAppService)
    {
        _creditCardAppService = creditCardAppService;
    }

    public async Task<CreditCardDetailDto?> Handle(GetCreditCardDetailQuery request, CancellationToken cancellationToken)
    {
        return await _creditCardAppService.GetCreditCardDetailByIdAsync(request.Id);
    }
}
