using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.DTOs.Identity;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Application.Tests;

public class UserAppServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IApplicationUserRepository> _userRepoMock;
    private readonly Mock<ISavingsAccountRepository> _savingsAccountRepoMock;
    private readonly Mock<ILoanRepository> _loanRepoMock;
    private readonly Mock<IMerchantRepository> _merchantRepoMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IAuthAppService> _authAppServiceMock;
    private readonly UserAppService _userService;

    public UserAppServiceTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IApplicationUserRepository>();
        _savingsAccountRepoMock = new Mock<ISavingsAccountRepository>();
        _loanRepoMock = new Mock<ILoanRepository>();
        _merchantRepoMock = new Mock<IMerchantRepository>();
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _mapperMock = new Mock<IMapper>();
        _authAppServiceMock = new Mock<IAuthAppService>();

        _unitOfWorkMock.SetupGet(u => u.Users).Returns(_userRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.SavingsAccounts).Returns(_savingsAccountRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Loans).Returns(_loanRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Merchants).Returns(_merchantRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Transactions).Returns(_transactionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _userService = new UserAppService(
            _userManagerMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _authAppServiceMock.Object);
    }

    [Fact]
    public async Task GetAllUsersAsync_ExcludesCommerceUsers()
    {
        var userAdmin = new ApplicationUser { Id = Guid.NewGuid(), UserName = "admin" };
        var userCommerce = new ApplicationUser { Id = Guid.NewGuid(), UserName = "merchant" };
        var users = new List<ApplicationUser> { userAdmin, userCommerce };

        _unitOfWorkMock.Setup(u => u.Users.GetAllAsync()).ReturnsAsync(users);
        _userManagerMock.Setup(m => m.GetRolesAsync(userAdmin)).ReturnsAsync(new List<string> { "Administrador" });
        _userManagerMock.Setup(m => m.GetRolesAsync(userCommerce)).ReturnsAsync(new List<string> { "Comercio" });
        _mapperMock.Setup(m => m.Map<UserDto>(userAdmin)).Returns(new UserDto { UserName = "admin" });

        var result = (await _userService.GetAllUsersAsync()).ToList();

        Assert.Single(result);
        Assert.Equal("admin", result[0].UserName);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateCedula_ReturnsError()
    {
        _userRepoMock.Setup(u => u.FindAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(new List<ApplicationUser> { new() { Cedula = "40200000001" } });

        var dto = new CreateUserDto { Cedula = "40200000001", Email = "new@test.com", UserName = "newuser", Role = "Cliente" };
        var (success, error) = await _userService.CreateUserAsync(dto, "format");

        Assert.False(success);
        Assert.Equal("Ya existe un usuario registrado con esta cédula.", error);
    }

    [Fact]
    public async Task CreateUserAsync_ValidClient_CreatesUserAndPrimaryAccount()
    {
        var createdUser = new ApplicationUser { Id = Guid.NewGuid(), Email = "client@test.com", FirstName = "Pedro" };

        _userRepoMock.Setup(u => u.FindAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(new List<ApplicationUser>());
        _userManagerMock.Setup(m => m.FindByEmailAsync("client@test.com")).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.FindByNameAsync("client_pedro")).ReturnsAsync((ApplicationUser?)null);

        _authAppServiceMock.Setup(a => a.RegisterAsync(It.IsAny<CreateUserDto>(), It.IsAny<string>(), false))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.SetupSequence(m => m.FindByEmailAsync("client@test.com"))
            .ReturnsAsync((ApplicationUser?)null)
            .ReturnsAsync(createdUser);

        _savingsAccountRepoMock.Setup(s => s.ExistsAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
            .ReturnsAsync(false);
        _loanRepoMock.Setup(l => l.ExistsAsync(It.IsAny<Expression<Func<Loan, bool>>>()))
            .ReturnsAsync(false);

        var dto = new CreateUserDto
        {
            Cedula = "40200000002",
            Email = "client@test.com",
            UserName = "client_pedro",
            Role = "Cliente",
            InitialBalance = 1500m
        };

        var (success, error) = await _userService.CreateUserAsync(dto, "format");

        Assert.True(success);
        Assert.Null(error);
        _savingsAccountRepoMock.Verify(s => s.AddAsync(It.Is<SavingsAccount>(a => a.ClientId == createdUser.Id && a.AccountType == AccountType.Principal && a.Balance == 1500m)), Times.Once);
        _transactionRepoMock.Verify(t => t.AddAsync(It.Is<Transaction>(tx => tx.Amount == 1500m && tx.Type == TransactionType.CRÉDITO)), Times.Once);
    }

    [Fact]
    public async Task ToggleUserStatusAsync_ExistingUser_InvertsActiveState()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, IsActive = true };
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var (success, error) = await _userService.ToggleUserStatusAsync(userId);

        Assert.True(success);
        Assert.Null(error);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task UpdateUserStatusApiAsync_SelfModification_ReturnsForbidden()
    {
        var adminId = Guid.NewGuid();
        var (success, code, error) = await _userService.UpdateUserStatusApiAsync(adminId, false, adminId);

        Assert.False(success);
        Assert.Equal("Forbidden", code);
        Assert.Equal("El administrador no puede modificar su propio estado.", error);
    }

    [Fact]
    public async Task CreateCommerceUserApiAsync_CommerceAlreadyHasUser_ReturnsConflict()
    {
        var commerceId = Guid.NewGuid();
        var merchant = new Merchant
        {
            Id = commerceId,
            Name = "Supermercado XYZ",
            Users = new List<ApplicationUser> { new() { Id = Guid.NewGuid() } }
        };

        _merchantRepoMock.Setup(m => m.GetByIdWithUsersAsync(commerceId)).ReturnsAsync(merchant);

        var dto = new CreateCommerceUserApiDto
        {
            Identification = "40299999999",
            Email = "merc@test.com",
            UserName = "mercuser",
            FirstName = "Com",
            LastName = "User",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            InitialAmount = 1000m
        };

        var (success, code, error, user) = await _userService.CreateCommerceUserApiAsync(commerceId, dto);

        Assert.False(success);
        Assert.Equal("Conflict", code);
        Assert.Equal("El comercio ya tiene un usuario asociado.", error);
    }

    [Fact]
    public async Task GetUserDetailApiAsync_ExistingUser_ReturnsDetailWithMainAccount()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "johndoe",
            Cedula = "40212345678",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var mainAccount = new SavingsAccount
        {
            Id = Guid.NewGuid(),
            ClientId = userId,
            AccountNumber = "987654321",
            Balance = 25000m,
            AccountType = AccountType.Principal,
            Status = AccountStatus.Activa
        };

        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Cliente" });
        _savingsAccountRepoMock.Setup(s => s.FindAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
            .ReturnsAsync(new List<SavingsAccount> { mainAccount });

        var result = await _userService.GetUserDetailApiAsync(userId);

        Assert.NotNull(result);
        Assert.Equal("johndoe", result.UserName);
        Assert.NotNull(result.MainAccount);
        Assert.Equal("987654321", result.MainAccount.AccountNumber);
        Assert.Equal(25000m, result.MainAccount.Balance);
    }
}
