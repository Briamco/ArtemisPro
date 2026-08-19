using Application.Interfaces.Services;
using Application.Mappings;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<IUserAppService, UserAppService>();
        services.AddScoped<ILoanAppService, LoanAppService>();
        services.AddScoped<ISavingsAccountAppService, Application.Services.Banking.SavingsAccountAppService>();
        services.AddScoped<IAdminDashboardAppService, AdminDashboardAppService>();
        services.AddScoped<IPaymentAppService, PaymentAppService>();
        services.AddScoped<ICreditCardAppService, CreditCardAppService>();
        services.AddScoped<IBeneficiaryAppService, Application.Services.Banking.BeneficiaryAppService>();
        services.AddScoped<ITransferAppService, Application.Services.Banking.TransferAppService>();
        services.AddScoped<IDepositAppService, Application.Services.Banking.DepositAppService>();
        services.AddScoped<IWithdrawalAppService, Application.Services.Banking.WithdrawalAppService>();
        services.AddScoped<ICardPaymentAppService, Application.Services.Banking.CardPaymentAppService>();
        services.AddScoped<ILoanPaymentAppService, Application.Services.Banking.LoanPaymentAppService>();
        services.AddScoped<IThirdPartyTransactionAppService, Application.Services.Banking.ThirdPartyTransactionAppService>();
        services.AddScoped<ICashierDashboardAppService, CashierDashboardAppService>();
        services.AddScoped<ICommerceAppService, Application.Services.Banking.CommerceAppService>();
        services.AddScoped<IHermesPayAppService, Application.Services.Banking.HermesPayAppService>();
        services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);
        return services;
    }
}
