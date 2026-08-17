using System;
using System.Collections.Generic;
using Application.DTOs.Banking;
using Application.Mappings;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Tests;

public class AutoMapperProfileTests
{
    private readonly IMapper _mapper;

    public AutoMapperProfileTests()
    {
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>()).CreateMapper();
    }

    private static Loan BuildLoanWithInstallments(params LoanInstallment[] installments)
    {
        return new Loan
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            LoanNumber = "111222333",
            ApprovedAmount = 3000m,
            Term = 3,
            AnnualInterestRate = 12m,
            Status = LoanStatus.Activo,
            AdminId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Client = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FirstName = "María",
                LastName = "López"
            },
            Installments = installments
        };
    }

    private static LoanInstallment BuildInstallment(PaymentStatus status, decimal amount, decimal pendingBalance)
    {
        return new LoanInstallment
        {
            Id = Guid.NewGuid(),
            InstallmentNumber = 1,
            DueDate = DateTime.UtcNow,
            Amount = amount,
            PendingBalance = pendingBalance,
            PaymentStatus = status,
            IsOverdue = false
        };
    }

    [Fact]
    public void MapLoan_PartiallyPaidInstallment_PendingAmountSumsPendingBalance()
    {
        // Arrange
        var loan = BuildLoanWithInstallments(
            BuildInstallment(PaymentStatus.Pagada, 1000m, 0m),
            BuildInstallment(PaymentStatus.Parcial, 1000m, 400m),
            BuildInstallment(PaymentStatus.Pendiente, 1000m, 1000m));

        // Act
        var dto = _mapper.Map<LoanDto>(loan);

        // Assert
        Assert.Equal(1400m, dto.PendingAmount);
    }

    [Fact]
    public void MapLoan_FullyPaid_PendingAmountIsZero()
    {
        // Arrange
        var loan = BuildLoanWithInstallments(
            BuildInstallment(PaymentStatus.Pagada, 1000m, 0m),
            BuildInstallment(PaymentStatus.Pagada, 1000m, 0m));

        // Act
        var dto = _mapper.Map<LoanDto>(loan);

        // Assert
        Assert.Equal(0m, dto.PendingAmount);
    }
}
