using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Services.Banking;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Application.Tests;

public class CommerceAppServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IMerchantRepository> _merchants;
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly CommerceAppService _service;

    public CommerceAppServiceTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _merchants = new Mock<IMerchantRepository>();
        
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);

        _unitOfWork.SetupGet(u => u.Merchants).Returns(_merchants.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _service = new CommerceAppService(_unitOfWork.Object, _userManager.Object);
    }

    [Fact]
    public async Task CreateCommerce_UniqueData_CreatesActiveMerchantSuccessfully()
    {
        var dto = new CreateCommerceDto
        {
            Name = "Supermercado Central",
            Email = "super@central.com",
            PhoneNumber = "8095551234",
            RNC = "101000222"
        };

        _merchants.Setup(m => m.GetByRNCAsync(dto.RNC)).ReturnsAsync((Merchant?)null);
        _merchants.Setup(m => m.GetByEmailAsync(dto.Email)).ReturnsAsync((Merchant?)null);

        var (success, errorCode, errorMessage, commerce) = await _service.CreateCommerceAsync(dto, Guid.NewGuid());

        Assert.True(success);
        Assert.Null(errorCode);
        Assert.NotNull(commerce);
        Assert.Equal("Supermercado Central", commerce.Name);
        Assert.True(commerce.IsActive);
        _merchants.Verify(m => m.AddAsync(It.Is<Merchant>(mer => mer.Name == dto.Name && mer.Status == MerchantStatus.Activo)), Times.Once);
    }

    [Fact]
    public async Task CreateCommerce_DuplicateRnc_ReturnsConflict()
    {
        var dto = new CreateCommerceDto
        {
            Name = "Supermercado Central",
            Email = "super@central.com",
            PhoneNumber = "8095551234",
            RNC = "101000222"
        };

        _merchants.Setup(m => m.GetByRNCAsync(dto.RNC)).ReturnsAsync(new Merchant { RNC = dto.RNC });

        var (success, errorCode, errorMessage, commerce) = await _service.CreateCommerceAsync(dto, Guid.NewGuid());

        Assert.False(success);
        Assert.Equal("Conflict", errorCode);
        Assert.Contains("RNC", errorMessage);
    }

    [Fact]
    public async Task UpdateCommerceStatus_Deactivate_InactivatesAssociatedUsers()
    {
        var merchantId = Guid.NewGuid();
        var user1 = new ApplicationUser { Id = Guid.NewGuid(), IsActive = true, MerchantId = merchantId };
        var merchant = new Merchant
        {
            Id = merchantId,
            Name = "Comercio ABC",
            Status = MerchantStatus.Activo,
            Users = new List<ApplicationUser> { user1 }
        };

        _merchants.Setup(m => m.GetByIdWithUsersAsync(merchantId)).ReturnsAsync(merchant);
        _userManager.Setup(u => u.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        var (success, errorCode, errorMessage) = await _service.UpdateCommerceStatusAsync(merchantId, false);

        Assert.True(success);
        Assert.Equal(MerchantStatus.Inactivo, merchant.Status);
        Assert.False(user1.IsActive);
        _userManager.Verify(u => u.UpdateAsync(user1), Times.Once);
    }

    [Fact]
    public async Task UpdateCommerceStatus_Reactivate_DoesNotReactivateUsers()
    {
        var merchantId = Guid.NewGuid();
        var user1 = new ApplicationUser { Id = Guid.NewGuid(), IsActive = false, MerchantId = merchantId };
        var merchant = new Merchant
        {
            Id = merchantId,
            Name = "Comercio ABC",
            Status = MerchantStatus.Inactivo,
            Users = new List<ApplicationUser> { user1 }
        };

        _merchants.Setup(m => m.GetByIdWithUsersAsync(merchantId)).ReturnsAsync(merchant);

        var (success, errorCode, errorMessage) = await _service.UpdateCommerceStatusAsync(merchantId, true);

        Assert.True(success);
        Assert.Equal(MerchantStatus.Activo, merchant.Status);
        Assert.False(user1.IsActive);
        _userManager.Verify(u => u.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }
}
