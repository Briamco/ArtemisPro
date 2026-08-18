namespace Application.Mappings;

using AutoMapper;
using Domain.Entities;
using Application.DTOs.Identity;
using Application.DTOs.Banking;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // Identity
        CreateMap<ApplicationUser, UserDto>()
            .ForMember(dest => dest.Role, opt => opt.Ignore());
        CreateMap<CreateUserDto, ApplicationUser>();

        // Banking
        CreateMap<SavingsAccount, SavingsAccountDto>()
            .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => $"{src.Client.FirstName} {src.Client.LastName}"));

        CreateMap<Loan, LoanDto>()
            .ForMember(dest => dest.ClientFullName, opt => opt.MapFrom(src => $"{src.Client.FirstName} {src.Client.LastName}"))
            .ForMember(dest => dest.CapitalAmount, opt => opt.MapFrom(src => src.ApprovedAmount))
            .ForMember(dest => dest.TermInMonths, opt => opt.MapFrom(src => src.Term))
            .ForMember(dest => dest.PendingAmount, opt => opt.MapFrom(src => src.Installments.Where(i => i.PaymentStatus != Domain.Enums.PaymentStatus.Pagada).Sum(i => i.PendingBalance)))
            .ForMember(dest => dest.ClientPaymentStatus, opt => opt.MapFrom(src => src.Installments.Any(i => i.IsOverdue) ? "En mora" : "Al día"));

        CreateMap<Loan, LoanDetailDto>()
            .ForMember(dest => dest.ClientFullName, opt => opt.MapFrom(src => $"{src.Client.FirstName} {src.Client.LastName}"))
            .ForMember(dest => dest.CapitalAmount, opt => opt.MapFrom(src => src.ApprovedAmount))
            .ForMember(dest => dest.TermInMonths, opt => opt.MapFrom(src => src.Term))
            .ForMember(dest => dest.PendingAmount, opt => opt.MapFrom(src => src.Installments.Where(i => i.PaymentStatus != Domain.Enums.PaymentStatus.Pagada).Sum(i => i.PendingBalance)))
            .ForMember(dest => dest.ClientPaymentStatus, opt => opt.MapFrom(src => src.Installments.Any(i => i.IsOverdue) ? "En mora" : "Al día"))
            .ForMember(dest => dest.MonthlyInstallment, opt => opt.MapFrom(src => src.Installments.Any() ? src.Installments.First().Amount : 0))
            .ForMember(dest => dest.Amortization, opt => opt.MapFrom(src => src.Installments.OrderBy(i => i.InstallmentNumber)));

        CreateMap<LoanInstallment, LoanInstallmentDto>()
            .ForMember(dest => dest.InstallmentAmount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.PendingInstallmentAmount, opt => opt.MapFrom(src => src.PendingBalance))
            .ForMember(dest => dest.IsLate, opt => opt.MapFrom(src => src.IsOverdue));

        CreateMap<LoanInstallment, LoanAmortizationRowDto>()
            .ForMember(dest => dest.InstallmentAmount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.PendingInstallmentAmount, opt => opt.MapFrom(src => src.PendingBalance))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus.ToString()))
            .ForMember(dest => dest.IsLate, opt => opt.MapFrom(src => src.IsOverdue));

        CreateMap<CreditCard, CreditCardDto>()
            .ForMember(dest => dest.MaskedCardNumber, opt => opt.MapFrom(src => "****-****-****-" + src.CardNumber.Substring(src.CardNumber.Length - 4)))
            .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => $"{src.Client.FirstName} {src.Client.LastName}"));

        CreateMap<CreditCardTransaction, CreditCardTransactionDto>();

        CreateMap<Transaction, TransactionDto>();
        CreateMap<Beneficiary, BeneficiaryDto>();
    }
}
