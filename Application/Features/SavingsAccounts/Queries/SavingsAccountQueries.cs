using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.SavingsAccounts.Queries;

public record GetSavingsAccountsPagedQuery(
    int Page, int PageSize, string? Status, string? Type, string? Identification) : IRequest<PagedResultDto<SavingsAccountApiDto>>;

public class GetSavingsAccountsPagedQueryValidator : AbstractValidator<GetSavingsAccountsPagedQuery>
{
    public GetSavingsAccountsPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");
        RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
            .LessThanOrEqualTo(20).WithMessage("El valor máximo permitido para pageSize debe ser 20.");
    }
}

public class GetSavingsAccountsPagedQueryHandler : IRequestHandler<GetSavingsAccountsPagedQuery, PagedResultDto<SavingsAccountApiDto>>
{
    private readonly ISavingsAccountAppService _savingsAccountAppService;

    public GetSavingsAccountsPagedQueryHandler(ISavingsAccountAppService savingsAccountAppService)
    {
        _savingsAccountAppService = savingsAccountAppService;
    }

    public async Task<PagedResultDto<SavingsAccountApiDto>> Handle(GetSavingsAccountsPagedQuery request, CancellationToken cancellationToken)
    {
        return await _savingsAccountAppService.GetSavingsAccountsPagedApiAsync(
            request.Page, request.PageSize, request.Status, request.Type, request.Identification);
    }
}

public record GetSavingsAccountTransactionsQuery(string AccountNumber, int Page, int PageSize) : IRequest<SavingsAccountDetailWithTransactionsApiDto?>;

public class GetSavingsAccountTransactionsQueryValidator : AbstractValidator<GetSavingsAccountTransactionsQuery>
{
    public GetSavingsAccountTransactionsQueryValidator()
    {
        RuleFor(x => x.AccountNumber).NotEmpty().WithMessage("El número de cuenta es requerido.");
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");
        RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
            .LessThanOrEqualTo(20).WithMessage("El valor máximo permitido para pageSize debe ser 20.");
    }
}

public class GetSavingsAccountTransactionsQueryHandler : IRequestHandler<GetSavingsAccountTransactionsQuery, SavingsAccountDetailWithTransactionsApiDto?>
{
    private readonly ISavingsAccountAppService _savingsAccountAppService;

    public GetSavingsAccountTransactionsQueryHandler(ISavingsAccountAppService savingsAccountAppService)
    {
        _savingsAccountAppService = savingsAccountAppService;
    }

    public async Task<SavingsAccountDetailWithTransactionsApiDto?> Handle(GetSavingsAccountTransactionsQuery request, CancellationToken cancellationToken)
    {
        return await _savingsAccountAppService.GetAccountTransactionsApiAsync(request.AccountNumber, request.Page, request.PageSize);
    }
}
