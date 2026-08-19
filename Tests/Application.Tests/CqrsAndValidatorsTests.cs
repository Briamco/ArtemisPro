using System;
using Application.DTOs.Banking;
using Application.DTOs.Identity;
using Application.Features.Account.Commands;
using Application.Features.Commerce.Commands;
using Application.Features.Commerce.Queries;
using Application.Features.CreditCards.Commands;
using Application.Features.CreditCards.Queries;
using Application.Features.HermesPay.Commands;
using Application.Features.HermesPay.Queries;
using Application.Features.Loans.Commands;
using Application.Features.Loans.Queries;
using Application.Features.SavingsAccounts.Commands;
using Application.Features.SavingsAccounts.Queries;
using Application.Features.Users.Commands;
using Application.Features.Users.Queries;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests;

public class CqrsAndValidatorsTests
{
    [Fact]
    public void LoginCommandValidator_EmptyCredentials_ShouldHaveValidationError()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand(new LoginDto { UserName = "", Password = "" });
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.UserName);
        result.ShouldHaveValidationErrorFor(x => x.Dto.Password);
    }

    [Fact]
    public void ResetPasswordCommandValidator_MismatchedPasswords_ShouldHaveValidationError()
    {
        var validator = new ResetPasswordCommandValidator();
        var command = new ResetPasswordCommand("user-1", "token-1", "Password123!", "Different123!");
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    [Fact]
    public void CreateUserCommandValidator_EmptyFields_ShouldHaveValidationErrors()
    {
        var validator = new CreateUserCommandValidator();
        var command = new CreateUserCommand(new CreateUserApiDto
        {
            FirstName = "",
            LastName = "",
            Identification = "",
            Email = "invalid-email",
            UserName = "",
            Password = "Password1!",
            ConfirmPassword = "Mismatch!",
            Role = ""
        });
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.FirstName);
        result.ShouldHaveValidationErrorFor(x => x.Dto.LastName);
        result.ShouldHaveValidationErrorFor(x => x.Dto.Identification);
        result.ShouldHaveValidationErrorFor(x => x.Dto.Email);
        result.ShouldHaveValidationErrorFor(x => x.Dto.UserName);
        result.ShouldHaveValidationErrorFor(x => x.Dto.ConfirmPassword);
        result.ShouldHaveValidationErrorFor(x => x.Dto.Role);
    }

    [Fact]
    public void GetUsersPagedQueryValidator_InvalidPagination_ShouldHaveValidationErrors()
    {
        var validator = new GetUsersPagedQueryValidator();
        var command = new GetUsersPagedQuery(0, 50, null);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Page);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void CreateCommerceCommandValidator_ValidData_ShouldNotHaveValidationErrors()
    {
        var validator = new CreateCommerceCommandValidator();
        var command = new CreateCommerceCommand(new CreateCommerceDto
        {
            Name = "Colmado San Juan",
            Email = "colmado@sanjuan.com",
            PhoneNumber = "8095550000",
            RNC = "101234567"
        }, Guid.NewGuid());
        var result = validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ProcessPaymentCommandValidator_InvalidCardNumberOrAmount_ShouldHaveValidationErrors()
    {
        var validator = new ProcessPaymentCommandValidator();
        var command = new ProcessPaymentCommand(Guid.NewGuid(), new ProcessPaymentDto
        {
            CardNumber = "123", // Short
            MonthExpirationCard = "05",
            YearExpirationCard = "2028",
            Cvc = "1", // Short
            TransactionAmount = 0m // Zero
        });
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.CardNumber);
        result.ShouldHaveValidationErrorFor(x => x.Dto.Cvc);
        result.ShouldHaveValidationErrorFor(x => x.Dto.TransactionAmount);
    }

    [Fact]
    public void CreateLoanCommandValidator_InvalidTerm_ShouldHaveValidationError()
    {
        var validator = new CreateLoanCommandValidator();
        var command = new CreateLoanCommand(new CreateLoanDto
        {
            ClientId = Guid.NewGuid(),
            CapitalAmount = 10000m,
            AnnualInterestRate = 15m,
            TermInMonths = 7 // Invalid term (must be 6, 12, 18, 24, ...)
        }, Guid.NewGuid());
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.TermInMonths);
    }

    [Fact]
    public void CreateSavingsAccountCommandValidator_NegativeBalance_ShouldHaveValidationError()
    {
        var validator = new CreateSavingsAccountCommandValidator();
        var command = new CreateSavingsAccountCommand(new CreateSavingsAccountApiDto
        {
            ClientId = "client-1",
            InitialBalance = -100m
        }, Guid.NewGuid());
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.InitialBalance);
    }
}
