using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Services.Banking;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Moq;
using Xunit;

namespace Application.Tests;

public class BeneficiaryAppServiceTests
{
    private readonly Mock<IBeneficiaryRepository> _beneficiaryRepo;
    private readonly Mock<ISavingsAccountRepository> _savingsAccountRepo;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IMapper> _mapper;
    private readonly BeneficiaryAppService _service;

    public BeneficiaryAppServiceTests()
    {
        _beneficiaryRepo = new Mock<IBeneficiaryRepository>();
        _savingsAccountRepo = new Mock<ISavingsAccountRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _mapper = new Mock<IMapper>();

        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _service = new BeneficiaryAppService(
            _beneficiaryRepo.Object,
            _savingsAccountRepo.Object,
            _unitOfWork.Object,
            _mapper.Object);
    }

    [Fact]
    public async Task CreateBeneficiary_ValidThirdPartyAccount_CreatesSuccessfully()
    {
        var clientId = Guid.NewGuid();
        var thirdPartyClientId = Guid.NewGuid();
        var accountNumber = "123456789";

        var account = new SavingsAccount
        {
            AccountNumber = accountNumber,
            ClientId = thirdPartyClientId,
            Status = AccountStatus.Activa
        };

        _savingsAccountRepo.Setup(r => r.GetByAccountNumberAsync(accountNumber))
            .ReturnsAsync(account);
        _beneficiaryRepo.Setup(r => r.GetByClientAndAccountAsync(clientId, accountNumber))
            .ReturnsAsync((Beneficiary?)null);

        var dto = new CreateBeneficiaryDto
        {
            BeneficiaryAccountNumber = accountNumber,
            Alias = "Amigo"
        };

        var (success, error) = await _service.CreateBeneficiaryAsync(clientId, dto);

        Assert.True(success);
        Assert.Null(error);
        _beneficiaryRepo.Verify(r => r.AddAsync(It.Is<Beneficiary>(b => b.ClientId == clientId && b.BeneficiaryAccountNumber == accountNumber)), Times.Once);
    }

    [Fact]
    public async Task CreateBeneficiary_OwnAccount_ReturnsError()
    {
        var clientId = Guid.NewGuid();
        var accountNumber = "123456789";

        var ownAccount = new SavingsAccount
        {
            AccountNumber = accountNumber,
            ClientId = clientId,
            Status = AccountStatus.Activa
        };

        _savingsAccountRepo.Setup(r => r.GetByAccountNumberAsync(accountNumber))
            .ReturnsAsync(ownAccount);

        var dto = new CreateBeneficiaryDto
        {
            BeneficiaryAccountNumber = accountNumber,
            Alias = "Mi otra cuenta"
        };

        var (success, error) = await _service.CreateBeneficiaryAsync(clientId, dto);

        Assert.False(success);
        Assert.Contains("cuenta propia", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBeneficiary_CancelledAccount_ReturnsError()
    {
        var clientId = Guid.NewGuid();
        var thirdPartyClientId = Guid.NewGuid();
        var accountNumber = "123456789";

        var cancelledAccount = new SavingsAccount
        {
            AccountNumber = accountNumber,
            ClientId = thirdPartyClientId,
            Status = AccountStatus.Cancelada
        };

        _savingsAccountRepo.Setup(r => r.GetByAccountNumberAsync(accountNumber))
            .ReturnsAsync(cancelledAccount);

        var dto = new CreateBeneficiaryDto
        {
            BeneficiaryAccountNumber = accountNumber,
            Alias = "Cuenta Cancelada"
        };

        var (success, error) = await _service.CreateBeneficiaryAsync(clientId, dto);

        Assert.False(success);
        Assert.Contains("cancelada", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBeneficiary_DuplicateBeneficiary_ReturnsError()
    {
        var clientId = Guid.NewGuid();
        var thirdPartyClientId = Guid.NewGuid();
        var accountNumber = "123456789";

        var account = new SavingsAccount
        {
            AccountNumber = accountNumber,
            ClientId = thirdPartyClientId,
            Status = AccountStatus.Activa
        };

        _savingsAccountRepo.Setup(r => r.GetByAccountNumberAsync(accountNumber))
            .ReturnsAsync(account);
        _beneficiaryRepo.Setup(r => r.GetByClientAndAccountAsync(clientId, accountNumber))
            .ReturnsAsync(new Beneficiary { ClientId = clientId, BeneficiaryAccountNumber = accountNumber });

        var dto = new CreateBeneficiaryDto
        {
            BeneficiaryAccountNumber = accountNumber,
            Alias = "Ya Registrado"
        };

        var (success, error) = await _service.CreateBeneficiaryAsync(clientId, dto);

        Assert.False(success);
        Assert.Contains("ya se encuentra registrada", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteBeneficiary_ExistingBeneficiary_DeletesSuccessfully()
    {
        var beneficiaryId = Guid.NewGuid();
        var beneficiary = new Beneficiary { Id = beneficiaryId };

        _beneficiaryRepo.Setup(r => r.GetByIdAsync(beneficiaryId))
            .ReturnsAsync(beneficiary);

        var (success, error) = await _service.DeleteBeneficiaryAsync(beneficiaryId);

        Assert.True(success);
        Assert.Null(error);
        _beneficiaryRepo.Verify(r => r.Delete(beneficiary), Times.Once);
    }
}
