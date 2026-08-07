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
            .ForMember(dest => dest.TotalInstallments, opt => opt.MapFrom(src => src.Installments.Count))
            .ForMember(dest => dest.PaidInstallments, opt => opt.MapFrom(src => src.Installments.Count(i => i.PaymentStatus == Domain.Enums.PaymentStatus.Pagada)))
            .ForMember(dest => dest.PendingAmount, opt => opt.Ignore())
            .ForMember(dest => dest.ClientStatus, opt => opt.Ignore());

        CreateMap<LoanInstallment, LoanInstallmentDto>();

        CreateMap<CreditCard, CreditCardDto>()
            .ForMember(dest => dest.MaskedCardNumber, opt => opt.MapFrom(src => "****-****-****-" + src.CardNumber.Substring(src.CardNumber.Length - 4)))
            .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => $"{src.Client.FirstName} {src.Client.LastName}"));

        CreateMap<CreditCardTransaction, CreditCardTransactionDto>();

        CreateMap<Transaction, TransactionDto>();
        CreateMap<Beneficiary, BeneficiaryDto>();
    }
}
