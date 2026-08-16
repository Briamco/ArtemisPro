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
            .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => $"{src.Client.FirstName} {src.Client.LastName}"))
            .ForMember(dest => dest.PendingAmount, opt => opt.MapFrom(src => src.Installments.Where(i => i.PaymentStatus != Domain.Enums.PaymentStatus.Pagada).Sum(i => i.PendingBalance)))
            .ForMember(dest => dest.ClientStatus, opt => opt.MapFrom(src => src.Installments.Any(i => i.IsOverdue) ? "En mora" : "Al día"));

        CreateMap<LoanInstallment, LoanInstallmentDto>();

        CreateMap<CreditCard, CreditCardDto>()
            .ForMember(dest => dest.MaskedCardNumber, opt => opt.MapFrom(src => "****-****-****-" + src.CardNumber.Substring(src.CardNumber.Length - 4)))
            .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => $"{src.Client.FirstName} {src.Client.LastName}"));

        CreateMap<CreditCardTransaction, CreditCardTransactionDto>();

        CreateMap<Transaction, TransactionDto>();
        CreateMap<Beneficiary, BeneficiaryDto>();
    }
}
