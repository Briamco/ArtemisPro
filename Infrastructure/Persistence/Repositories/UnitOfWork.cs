using System;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Persistence.Contexts;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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

    private IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await SaveChangesAsync();
            if (_transaction != null)
                await _transaction.CommitAsync();
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<int> GetActiveClientsCountAsync()
    {
        var clientRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Cliente");
        if (clientRole == null) return 0;
        
        return await _context.UserRoles
            .Where(ur => ur.RoleId == clientRole.Id)
            .Join(_context.Users, ur => ur.UserId, u => u.Id, (ur, u) => u)
            .Where(u => u.IsActive)
            .CountAsync();
    }
}
