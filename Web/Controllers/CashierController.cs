using System;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Services;
using Application.Models.ViewModels.Cashier;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Authorize(Roles = "Cajero")]
public class CashierController : Controller
{
    private readonly ICardPaymentAppService _cardPaymentService;
    private readonly IThirdPartyTransactionAppService _thirdPartyTransactionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CashierController(
        ICardPaymentAppService cardPaymentService,
        IThirdPartyTransactionAppService thirdPartyTransactionService,
        UserManager<ApplicationUser> userManager)
    {
        _cardPaymentService = cardPaymentService;
        _thirdPartyTransactionService = thirdPartyTransactionService;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Index()
    {
         //simulation 
        var model = new CashierHomeViewModel
        {
            TotalDepositsToday = 20,
            TotalWithdrawalsToday = 10,
            TotalPaymentsToday = 15, 
            TotalTransactionsToday = 45 
        };

        return View(model);
    }

    private static readonly List<CashierSystemAccountDto> _systemAccounts = new()
    {
        new CashierSystemAccountDto { AccountNumber = "111222333", FirstName = "Juan", LastName = "Pérez", Status = "Activa", Balance = 5000.00m },
        new CashierSystemAccountDto { AccountNumber = "444555666", FirstName = "María", LastName = "López", Status = "Inactiva", Balance = 1000.00m },
        new CashierSystemAccountDto { AccountNumber = "999888777", FirstName = "Carlos", LastName = "Ruiz", Status = "Activa", Balance = 0.00m }
    };

    private static readonly List<CashierSystemLoanDto> _systemLoans = new()
    {
        new CashierSystemLoanDto { LoanNumber = "111222333", FirstName = "Pedro", LastName = "Martínez", Status = "Activo", PendingAmount = 2000.00m, HasPendingInstallments = true },
        new CashierSystemLoanDto { LoanNumber = "444555666", FirstName = "Laura", LastName = "Sánchez", Status = "Completado", PendingAmount = 0.00m, HasPendingInstallments = false },
        new CashierSystemLoanDto { LoanNumber = "777888999", FirstName = "José", LastName = "Reyes", Status = "Activo", PendingAmount = 50000.00m, HasPendingInstallments = false } 
    };

    // DEPOSITS 
    [HttpGet]
    public IActionResult Deposit()
    {
        return View(new DepositViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Deposit(DepositViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var account = _systemAccounts.FirstOrDefault(a => a.AccountNumber == model.AccountNumber);

        // validation for account existence and status
        if (account == null || account.Status != "Activa")
        {
            ModelState.AddModelError("AccountNumber", "El número de cuenta ingresado no corresponde a una cuenta válida.");
            return View(model);
        }

        var confirmModel = new ConfirmDepositViewModel
        {
            AccountNumber = account.AccountNumber,
            OwnerName = $"{account.FirstName} {account.LastName}",
            Amount = model.Amount
        };

        return View("ConfirmDeposit", confirmModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExecuteDeposit(ConfirmDepositViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Deposit));

        var account = _systemAccounts.FirstOrDefault(a => a.AccountNumber == model.AccountNumber);
        if (account == null || account.Status != "Activa" || model.Amount <= 0)
        {
            TempData["ErrorMessage"] = "No se pudo procesar el depósito. La cuenta ingresada no es válida o el monto es incorrecto.";
            return RedirectToAction(nameof(Deposit));
        }

        TempData["SuccessMessage"] = "El depósito fue realizado correctamente.";
        return RedirectToAction("Index"); 
    }

    // withdrawals
    [HttpGet]
    public IActionResult Withdrawal()
    {
        return View(new WithdrawalViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Withdrawal(WithdrawalViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var account = _systemAccounts.FirstOrDefault(a => a.AccountNumber == model.AccountNumber);

        // Validations
        if (account == null || account.Status != "Activa")
        {
            ModelState.AddModelError("AccountNumber", "El número de cuenta ingresado no corresponde a una cuenta válida.");
            return View(model);
        }

        if (account.Balance < model.Amount)
        {
            ModelState.AddModelError("Amount", "El monto ingresado excede el saldo disponible de la cuenta.");
            return View(model);
        }

        var confirmModel = new ConfirmWithdrawalViewModel
        {
            AccountNumber = account.AccountNumber,
            OwnerName = $"{account.FirstName} {account.LastName}",
            Amount = model.Amount
        };

        return View("ConfirmWithdrawal", confirmModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExecuteWithdrawal(ConfirmWithdrawalViewModel model)
    { 
        if (!ModelState.IsValid) return RedirectToAction(nameof(Withdrawal));

        var account = _systemAccounts.FirstOrDefault(a => a.AccountNumber == model.AccountNumber);
        if (account == null || account.Status != "Activa" || model.Amount <= 0 || account.Balance < model.Amount)
        {
            TempData["ErrorMessage"] = "No se pudo procesar el retiro. Verifique el estado de la cuenta y el saldo disponible.";
            return RedirectToAction(nameof(Withdrawal));
        }

        TempData["SuccessMessage"] = "El retiro fue realizado correctamente.";
        return RedirectToAction("Index"); 
    }

    // pay credit card 
    [HttpGet]
    public IActionResult PayCreditCard()
    {
        return View(new PayCreditCardViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayCreditCard(PayCreditCardViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _cardPaymentService.GetCardPaymentPreviewAsync(model.SourceAccountNumber, model.CreditCardNumber, model.Amount);

        if (!result.Success)
        {
            var error = result.Error ?? "No se pudo procesar el pago. Verifique los datos ingresados.";
            ModelState.AddModelError(
                error.Contains("tarjeta") ? "CreditCardNumber"
                    : error.Contains("cuenta") ? "SourceAccountNumber"
                    : "Amount",
                error);
            return View(model);
        }

        var preview = result.Preview!;
        var confirmModel = new ConfirmPayCreditCardViewModel
        {
            SourceAccountOwner = preview.OriginAccountClientName,
            SourceAccountNumber = preview.OriginAccountNumber,
            CreditCardOwner = preview.CardClientName,
            CreditCardNumber = model.CreditCardNumber,
            CreditCardMasked = $"**** {preview.CardLast4}",
            EnteredAmount = preview.EnteredAmount,
            EffectiveAmount = preview.EffectiveAmount
        };

        return View("ConfirmPayCreditCard", confirmModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecutePayCreditCard(ConfirmPayCreditCardViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(PayCreditCard));

        var teller = await _userManager.GetUserAsync(User);
        if (teller == null)
        {
            return RedirectToAction(nameof(PayCreditCard));
        }

        var dto = new CreateCardPaymentDto
        {
            AccountNumber = model.SourceAccountNumber,
            CardNumber = model.CreditCardNumber,
            Amount = model.EnteredAmount
        };

        var result = await _cardPaymentService.CreateCardPaymentAsync(teller.Id, dto);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Error ?? "No se pudo procesar el pago.";
            return RedirectToAction(nameof(PayCreditCard));
        }

        TempData["SuccessMessage"] = result.EmailSent
            ? "El pago fue realizado correctamente."
            : "El pago fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
        return RedirectToAction("Index");
    }

    // pay loan
    [HttpGet]
    public IActionResult PayLoan()
    {
        return View(new PayLoanViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PayLoan(PayLoanViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var account = _systemAccounts.FirstOrDefault(a => a.AccountNumber == model.SourceAccountNumber);
        var loan = _systemLoans.FirstOrDefault(l => l.LoanNumber == model.LoanNumber);

        // Validations
        if (account == null || account.Status != "Activa")
        {
            ModelState.AddModelError("SourceAccountNumber", "El número de cuenta ingresado no corresponde a una cuenta válida.");
            return View(model);
        }

        if (loan == null || loan.Status == "Completado")
        {
            ModelState.AddModelError("LoanNumber", "El número de préstamo ingresado no corresponde a un préstamo válido.");
            return View(model);
        }

        if (!loan.HasPendingInstallments || loan.PendingAmount <= 0)
        {
            ModelState.AddModelError("LoanNumber", "El préstamo seleccionado no tiene cuotas pendientes de pago.");
            return View(model);
        }

        decimal effectiveAmount = Math.Min(model.Amount, loan.PendingAmount);

        if (account.Balance < effectiveAmount)
        {
            ModelState.AddModelError("Amount", "El monto ingresado excede el saldo disponible de la cuenta.");
            return View(model);
        }

        var confirmModel = new ConfirmPayLoanViewModel
        {
            SourceAccountOwner = $"{account.FirstName} {account.LastName}",
            SourceAccountNumber = account.AccountNumber,
            LoanOwner = $"{loan.FirstName} {loan.LastName}",
            LoanNumber = loan.LoanNumber,
            EnteredAmount = model.Amount,
            EffectiveAmount = effectiveAmount
        };

        return View("ConfirmPayLoan", confirmModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExecutePayLoan(ConfirmPayLoanViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(PayLoan));

        var account = _systemAccounts.FirstOrDefault(a => a.AccountNumber == model.SourceAccountNumber);
        var loan = _systemLoans.FirstOrDefault(l => l.LoanNumber == model.LoanNumber);

        if (account == null || account.Status != "Activa" || loan == null || loan.Status == "Completado" || !loan.HasPendingInstallments || loan.PendingAmount <= 0)
        {
            TempData["ErrorMessage"] = "No se pudo procesar el pago. El préstamo o la cuenta no son válidos para pago.";
            return RedirectToAction(nameof(PayLoan));
        }

        decimal effectiveAmount = Math.Min(model.EnteredAmount, loan.PendingAmount);
        if (effectiveAmount <= 0 || account.Balance < effectiveAmount)
        {
            TempData["ErrorMessage"] = "El saldo de la cuenta no es suficiente para cubrir el monto del pago.";
            return RedirectToAction(nameof(PayLoan));
        }

        TempData["SuccessMessage"] = "El pago fue realizado correctamente.";   
        return RedirectToAction("Index"); 
    }

    // third party transfer
    [HttpGet]
    public IActionResult ThirdPartyTransfer()
    {
        return View(new ThirdPartyTransferViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThirdPartyTransfer(ThirdPartyTransferViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _thirdPartyTransactionService.GetPreviewAsync(
            model.SourceAccountNumber,
            model.DestinationAccountNumber,
            model.Amount);

        if (!result.Success)
        {
            var error = result.Error ?? "No se pudo realizar la transacción. Verifique los datos ingresados.";
            ModelState.AddModelError(
                error.Contains("origen") ? "SourceAccountNumber"
                    : error.Contains("destino") ? "DestinationAccountNumber"
                    : "Amount",
                error);
            return View(model);
        }

        var preview = result.Preview!;
        var confirmModel = new ConfirmThirdPartyTransferViewModel
        {
            SourceAccountOwner = preview.SourceAccountOwner,
            SourceAccountNumber = preview.SourceAccountNumber,
            DestinationAccountOwner = preview.DestinationAccountOwner,
            DestinationAccountNumber = preview.DestinationAccountNumber,
            Amount = preview.Amount
        };

        return View("ConfirmThirdPartyTransfer", confirmModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteThirdPartyTransfer(ConfirmThirdPartyTransferViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(ThirdPartyTransfer));

        var teller = await _userManager.GetUserAsync(User);
        if (teller == null)
        {
            return RedirectToAction(nameof(ThirdPartyTransfer));
        }

        var dto = new CreateThirdPartyTransactionDto
        {
            SourceAccountNumber = model.SourceAccountNumber,
            DestinationAccountNumber = model.DestinationAccountNumber,
            Amount = model.Amount
        };

        var result = await _thirdPartyTransactionService.CreateTransactionAsync(teller.Id, dto);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Error ?? "No se pudo realizar la transacción.";
            return RedirectToAction(nameof(ThirdPartyTransfer));
        }

        TempData["SuccessMessage"] = result.EmailSent
            ? "La transacción fue realizada correctamente."
            : "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.";
        return RedirectToAction("Index");
    }
}