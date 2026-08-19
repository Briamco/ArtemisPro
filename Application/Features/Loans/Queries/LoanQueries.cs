using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Loans.Queries;

public record GetLoansPagedQuery(int Page, int PageSize, string? Status, string? Identification) : IRequest<PagedResultDto<LoanDto>>;

public class GetLoansPagedQueryValidator : AbstractValidator<GetLoansPagedQuery>
{
    public GetLoansPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");
        RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
            .LessThanOrEqualTo(20).WithMessage("El valor máximo permitido para pageSize debe ser 20.");
    }
}

public class GetLoansPagedQueryHandler : IRequestHandler<GetLoansPagedQuery, PagedResultDto<LoanDto>>
{
    private readonly ILoanAppService _loanAppService;

    public GetLoansPagedQueryHandler(ILoanAppService loanAppService)
    {
        _loanAppService = loanAppService;
    }

    public async Task<PagedResultDto<LoanDto>> Handle(GetLoansPagedQuery request, CancellationToken cancellationToken)
    {
        return await _loanAppService.GetLoansAsync(request.Page, request.PageSize, request.Status, request.Identification);
    }
}

public record GetLoanByIdQuery(Guid Id) : IRequest<LoanDetailDto?>;

public class GetLoanByIdQueryHandler : IRequestHandler<GetLoanByIdQuery, LoanDetailDto?>
{
    private readonly ILoanAppService _loanAppService;

    public GetLoanByIdQueryHandler(ILoanAppService loanAppService)
    {
        _loanAppService = loanAppService;
    }

    public async Task<LoanDetailDto?> Handle(GetLoanByIdQuery request, CancellationToken cancellationToken)
    {
        return await _loanAppService.GetLoanByIdAsync(request.Id);
    }
}
