using System.Threading.Tasks;

namespace Application.Interfaces.Repositories;

public interface IUnitOfWork
{
    IApplicationUserRepository Users { get; }
    ISavingsAccountRepository SavingsAccounts { get; }
    ILoanRepository Loans { get; }
    ILoanInstallmentRepository LoanInstallments { get; }
    ICreditCardRepository CreditCards { get; }
    ICreditCardTransactionRepository CreditCardTransactions { get; }
    ITransactionRepository Transactions { get; }
    IBeneficiaryRepository Beneficiaries { get; }
    IPasswordResetTokenRepository PasswordResetTokens { get; }
    IMerchantRepository Merchants { get; }
    Task<int> SaveChangesAsync();
}
