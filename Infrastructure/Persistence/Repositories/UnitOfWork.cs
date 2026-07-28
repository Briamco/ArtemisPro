using System;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IApplicationUserRepository? _users;
    private ISavingsAccountRepository? _savingsAccounts;
    private ILoanRepository? _loans;
    private ILoanInstallmentRepository? _loanInstallments;
    private ICreditCardRepository? _creditCards;
    private ICreditCardTransactionRepository? _creditCardTransactions;
    private ITransactionRepository? _transactions;
    private IBeneficiaryRepository? _beneficiaries;
    private IPasswordResetTokenRepository? _passwordResetTokens;
    private IMerchantRepository? _merchants;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IApplicationUserRepository Users => _users ??= new ApplicationUserRepository(_context);
    public ISavingsAccountRepository SavingsAccounts => _savingsAccounts ??= new SavingsAccountRepository(_context);
    public ILoanRepository Loans => _loans ??= new LoanRepository(_context);
    public ILoanInstallmentRepository LoanInstallments => _loanInstallments ??= new LoanInstallmentRepository(_context);
    public ICreditCardRepository CreditCards => _creditCards ??= new CreditCardRepository(_context);
    public ICreditCardTransactionRepository CreditCardTransactions => _creditCardTransactions ??= new CreditCardTransactionRepository(_context);
    public ITransactionRepository Transactions => _transactions ??= new TransactionRepository(_context);
    public IBeneficiaryRepository Beneficiaries => _beneficiaries ??= new BeneficiaryRepository(_context);
    public IPasswordResetTokenRepository PasswordResetTokens => _passwordResetTokens ??= new PasswordResetTokenRepository(_context);
    public IMerchantRepository Merchants => _merchants ??= new MerchantRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
