using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Shared.Interfaces;
using Xunit;

namespace Application.Tests;

public class CreditCardAppServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ICreditCardRepository> _creditCards;
    private readonly Mock<ICreditCardTransactionRepository> _creditCardTransactions;
    private readonly Mock<IApplicationUserRepository> _users;
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly CreditCardAppService _service;

    public CreditCardAppServiceTests()
    {
        _creditCards = new Mock<ICreditCardRepository>();
        _creditCardTransactions = new Mock<ICreditCardTransactionRepository>();
        _users = new Mock<IApplicationUserRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _emailService = new Mock<IEmailService>();
        _httpContextAccessor = new Mock<IHttpContextAccessor>();

        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _unitOfWork.SetupGet(u => u.CreditCards).Returns(_creditCards.Object);
        _unitOfWork.SetupGet(u => u.CreditCardTransactions).Returns(_creditCardTransactions.Object);
        _unitOfWork.SetupGet(u => u.Users).Returns(_users.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _creditCards
            .Setup(r => r.AddAsync(It.IsAny<CreditCard>()))
            .ReturnsAsync((CreditCard c) => c);

        _service = new CreditCardAppService(
            _unitOfWork.Object,
            _userManager.Object,
            _emailService.Object,
            _httpContextAccessor.Object);
    }

    private static ApplicationUser BuildClient(Guid id, bool isActive = true)
    {
        return new ApplicationUser
        {
            Id = id,
            FirstName = "Maria",
            LastName = "Garcia",
            Email = "maria.garcia@example.com",
            IsActive = isActive
        };
    }

    private static CreditCard BuildCard(Guid clientId, decimal limit = 1000m, decimal debt = 0m, CardStatus status = CardStatus.Activa, Guid? id = null, DateTime? createdAt = null)
    {
        return new CreditCard
        {
            Id = id ?? Guid.NewGuid(),
            ClientId = clientId,
            CardNumber = "4111111111111234",
            Limit = limit,
            Debt = debt,
            ExpirationDate = "12/29",
            CvcHash = "abc123hash",
            Status = status,
            AdminId = Guid.NewGuid(),
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    [Fact]
    public async Task AssignCreditCardAsync_ValidClient_CreatesCardWithGeneratedNumber()
    {
        var clientId = Guid.NewGuid();
        var client = BuildClient(clientId);
        _userManager.Setup(u => u.FindByIdAsync(clientId.ToString())).ReturnsAsync(client);
        _creditCards.Setup(r => r.GetByCardNumberAsync(It.IsAny<string>())).ReturnsAsync((CreditCard?)null);
        _httpContextAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);
        _userManager.Setup(u => u.GetUsersInRoleAsync("Administrador")).ReturnsAsync(new List<ApplicationUser>
        {
            new() { Id = Guid.NewGuid() }
        });

        var dto = new AssignCreditCardDto { ClientId = clientId, Limit = 5000m };

        var (success, error, card) = await _service.AssignCreditCardAsync(dto);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(card);
        Assert.Equal(5000m, card.CreditLimit);
        Assert.Equal(0m, card.CurrentDebt);
        Assert.Equal("Activa", card.Status);
        Assert.Equal(clientId, card.ClientId);
        Assert.StartsWith("**** **** **** ", card.MaskedCardNumber);
        Assert.EndsWith(card.LastFourDigits, card.MaskedCardNumber);
        Assert.Equal(4, card.LastFourDigits.Length);
        _creditCards.Verify(r => r.AddAsync(It.IsAny<CreditCard>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AssignCreditCardAsync_NonExistentClient_ReturnsError()
    {
        var clientId = Guid.NewGuid();
        _userManager.Setup(u => u.FindByIdAsync(clientId.ToString())).ReturnsAsync((ApplicationUser?)null);

        var dto = new AssignCreditCardDto { ClientId = clientId, Limit = 5000m };
        var (success, error, card) = await _service.AssignCreditCardAsync(dto);

        Assert.False(success);
        Assert.Equal("Cliente no encontrado.", error);
        Assert.Null(card);
        _creditCards.Verify(r => r.AddAsync(It.IsAny<CreditCard>()), Times.Never);
    }

    [Fact]
    public async Task AssignCreditCardAsync_InactiveClient_ReturnsError()
    {
        var clientId = Guid.NewGuid();
        var client = BuildClient(clientId, isActive: false);
        _userManager.Setup(u => u.FindByIdAsync(clientId.ToString())).ReturnsAsync(client);

        var dto = new AssignCreditCardDto { ClientId = clientId, Limit = 5000m };
        var (success, error, card) = await _service.AssignCreditCardAsync(dto);

        Assert.False(success);
        Assert.Equal("El cliente no está activo.", error);
        Assert.Null(card);
    }

    [Fact]
    public async Task AssignCreditCardAsync_ZeroLimit_ReturnsError()
    {
        var clientId = Guid.NewGuid();
        var client = BuildClient(clientId);
        _userManager.Setup(u => u.FindByIdAsync(clientId.ToString())).ReturnsAsync(client);

        var dto = new AssignCreditCardDto { ClientId = clientId, Limit = 0m };
        var (success, error, card) = await _service.AssignCreditCardAsync(dto);

        Assert.False(success);
        Assert.Equal("El límite debe ser mayor a cero.", error);
        Assert.Null(card);
    }

    [Fact]
    public async Task UpdateCreditCardLimitAsync_ValidRequest_UpdatesLimit()
    {
        var card = BuildCard(Guid.NewGuid(), limit: 1000m, debt: 200m);
        _creditCards.Setup(r => r.GetByIdAsync(card.Id)).ReturnsAsync(card);
        _userManager.Setup(u => u.FindByIdAsync(card.ClientId.ToString())).ReturnsAsync(BuildClient(card.ClientId));

        var dto = new UpdateCreditCardLimitDto { NewLimit = 2000m };
        var (success, error) = await _service.UpdateCreditCardLimitAsync(card.Id, dto);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(2000m, card.Limit);
        _creditCards.Verify(r => r.Update(card), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateCreditCardLimitAsync_NewLimitBelowDebt_ReturnsError()
    {
        var card = BuildCard(Guid.NewGuid(), limit: 1000m, debt: 500m);
        _creditCards.Setup(r => r.GetByIdAsync(card.Id)).ReturnsAsync(card);

        var dto = new UpdateCreditCardLimitDto { NewLimit = 300m };
        var (success, error) = await _service.UpdateCreditCardLimitAsync(card.Id, dto);

        Assert.False(success);
        Assert.Equal("El límite no puede ser menor a la deuda actual.", error);
        Assert.Equal(1000m, card.Limit);
        _creditCards.Verify(r => r.Update(It.IsAny<CreditCard>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCreditCardLimitAsync_CancelledCard_ReturnsError()
    {
        var card = BuildCard(Guid.NewGuid(), limit: 1000m, debt: 0m, status: CardStatus.Cancelada);
        _creditCards.Setup(r => r.GetByIdAsync(card.Id)).ReturnsAsync(card);

        var dto = new UpdateCreditCardLimitDto { NewLimit = 2000m };
        var (success, error) = await _service.UpdateCreditCardLimitAsync(card.Id, dto);

        Assert.False(success);
        Assert.Equal("No se puede modificar el límite de una tarjeta cancelada.", error);
        _creditCards.Verify(r => r.Update(It.IsAny<CreditCard>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCreditCardLimitAsync_NonExistentCard_ReturnsNotFound()
    {
        _creditCards.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((CreditCard?)null);

        var dto = new UpdateCreditCardLimitDto { NewLimit = 2000m };
        var (success, error) = await _service.UpdateCreditCardLimitAsync(Guid.NewGuid(), dto);

        Assert.False(success);
        Assert.Equal("Tarjeta no encontrada.", error);
    }

    [Fact]
    public async Task UpdateCreditCardLimitAsync_ZeroLimit_ReturnsError()
    {
        var card = BuildCard(Guid.NewGuid(), limit: 1000m, debt: 0m);
        _creditCards.Setup(r => r.GetByIdAsync(card.Id)).ReturnsAsync(card);

        var dto = new UpdateCreditCardLimitDto { NewLimit = 0m };
        var (success, error) = await _service.UpdateCreditCardLimitAsync(card.Id, dto);

        Assert.False(success);
        Assert.Equal("El límite debe ser mayor a cero.", error);
    }

    [Fact]
    public async Task CancelCreditCardAsync_ActiveNoDebt_CancelsSuccessfully()
    {
        var card = BuildCard(Guid.NewGuid(), limit: 1000m, debt: 0m);
        _creditCards.Setup(r => r.GetByIdAsync(card.Id)).ReturnsAsync(card);

        var (success, error) = await _service.CancelCreditCardAsync(card.Id);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(CardStatus.Cancelada, card.Status);
        _creditCards.Verify(r => r.Update(card), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CancelCreditCardAsync_WithDebt_ReturnsError()
    {
        var card = BuildCard(Guid.NewGuid(), limit: 1000m, debt: 500m);
        _creditCards.Setup(r => r.GetByIdAsync(card.Id)).ReturnsAsync(card);

        var (success, error) = await _service.CancelCreditCardAsync(card.Id);

        Assert.False(success);
        Assert.Equal("No se puede cancelar una tarjeta con deuda.", error);
        Assert.Equal(CardStatus.Activa, card.Status);
        _creditCards.Verify(r => r.Update(It.IsAny<CreditCard>()), Times.Never);
    }

    [Fact]
    public async Task CancelCreditCardAsync_AlreadyCancelled_ReturnsError()
    {
        var card = BuildCard(Guid.NewGuid(), limit: 1000m, debt: 0m, status: CardStatus.Cancelada);
        _creditCards.Setup(r => r.GetByIdAsync(card.Id)).ReturnsAsync(card);

        var (success, error) = await _service.CancelCreditCardAsync(card.Id);

        Assert.False(success);
        Assert.Equal("La tarjeta ya está cancelada.", error);
        _creditCards.Verify(r => r.Update(It.IsAny<CreditCard>()), Times.Never);
    }

    [Fact]
    public async Task CancelCreditCardAsync_NonExistentCard_ReturnsNotFound()
    {
        _creditCards.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((CreditCard?)null);

        var (success, error) = await _service.CancelCreditCardAsync(Guid.NewGuid());

        Assert.False(success);
        Assert.Equal("Tarjeta no encontrada.", error);
    }

    [Fact]
    public async Task GetCreditCardByIdAsync_ExistingCard_ReturnsDtoWithMaskedNumber()
    {
        var clientId = Guid.NewGuid();
        var card = BuildCard(clientId, limit: 5000m, debt: 1200m);
        _creditCards.Setup(r => r.GetByIdAsync(card.Id)).ReturnsAsync(card);
        _userManager.Setup(u => u.FindByIdAsync(clientId.ToString())).ReturnsAsync(BuildClient(clientId));

        var result = await _service.GetCreditCardByIdAsync(card.Id);

        Assert.NotNull(result);
        Assert.Equal("**** **** **** 1234", result.MaskedCardNumber);
        Assert.Equal("1234", result.LastFourDigits);
        Assert.Equal(5000m, result.CreditLimit);
        Assert.Equal(1200m, result.CurrentDebt);
        Assert.Equal(3800m, result.AvailableCredit);
        Assert.Equal("Activa", result.Status);
    }

    [Fact]
    public async Task GetCreditCardByIdAsync_NonExistentCard_ReturnsNull()
    {
        _creditCards.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((CreditCard?)null);

        var result = await _service.GetCreditCardByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCreditCardsPagedAsync_DefaultFilter_ReturnsActiveCardsOrderedByCreatedAt()
    {
        var clientId = Guid.NewGuid();
        var client = BuildClient(clientId);

        var cards = new List<CreditCard>
        {
            BuildCard(clientId, limit: 1000m, createdAt: DateTime.UtcNow.AddDays(-2)),
            BuildCard(clientId, limit: 2000m, createdAt: DateTime.UtcNow.AddDays(-1))
        };

        _creditCards.Setup(r => r.GetPagedAsync(1, 20, null, null))
            .ReturnsAsync((cards, cards.Count));

        var result = await _service.GetCreditCardsPagedAsync(1, 20, null, null);

        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(2, result.Data.Count());
        Assert.All(result.Data, c => Assert.Equal("Activa", c.Status));
    }

    [Fact]
    public async Task GetCreditCardsPagedAsync_FilterByStatus_ReturnsOnlyMatchingCards()
    {
        var clientId = Guid.NewGuid();

        var cards = new List<CreditCard>
        {
            BuildCard(clientId, status: CardStatus.Cancelada)
        };

        _creditCards.Setup(r => r.GetPagedAsync(1, 20, CardStatus.Cancelada, null))
            .ReturnsAsync((cards, 1));

        var result = await _service.GetCreditCardsPagedAsync(1, 20, "Cancelada", null);

        Assert.Single(result.Data);
        Assert.Equal("Cancelada", result.Data.First().Status);
    }

    [Fact]
    public async Task GetCreditCardsPagedAsync_FilterByCedula_ReturnsOnlyMatchingClientCards()
    {
        var clientId = Guid.NewGuid();

        var cards = new List<CreditCard>
        {
            BuildCard(clientId)
        };

        _creditCards.Setup(r => r.GetPagedAsync(1, 20, null, "12345"))
            .ReturnsAsync((cards, 1));

        var result = await _service.GetCreditCardsPagedAsync(1, 20, null, "12345");

        Assert.Single(result.Data);
        Assert.Equal(clientId, result.Data.First().ClientId);
    }

    [Fact]
    public async Task GetCreditCardsPagedAsync_Pagination_ReturnsCorrectPage()
    {
        var clientId = Guid.NewGuid();
        var page1Cards = new List<CreditCard>
        {
            BuildCard(clientId, limit: 100m, createdAt: DateTime.UtcNow.AddMinutes(-1)),
            BuildCard(clientId, limit: 200m, createdAt: DateTime.UtcNow.AddMinutes(-2))
        };
        var page2Cards = new List<CreditCard>
        {
            BuildCard(clientId, limit: 300m, createdAt: DateTime.UtcNow.AddMinutes(-3))
        };

        _creditCards.Setup(r => r.GetPagedAsync(1, 2, null, null))
            .ReturnsAsync((page1Cards, 5));
        _creditCards.Setup(r => r.GetPagedAsync(2, 2, null, null))
            .ReturnsAsync((page2Cards, 5));

        var page1 = await _service.GetCreditCardsPagedAsync(1, 2, null, null);
        var page2 = await _service.GetCreditCardsPagedAsync(2, 2, null, null);

        Assert.Equal(5, page1.TotalRecords);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(2, page1.Data.Count());
        Assert.Single(page2.Data);
        Assert.NotEqual(page1.Data.First().Id, page2.Data.First().Id);
    }
}
