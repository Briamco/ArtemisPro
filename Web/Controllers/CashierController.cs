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
    private readonly ILoanPaymentAppService _loanPaymentService;
    private readonly IDepositAppService _depositService;
    private readonly IWithdrawalAppService _withdrawalService;
    private readonly Application.Interfaces.Repositories.IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public CashierController(
        ICardPaymentAppService cardPaymentService,
        IThirdPartyTransactionAppService thirdPartyTransactionService,
        ILoanPaymentAppService loanPaymentService,
        IDepositAppService depositService,
        IWithdrawalAppService withdrawalService,
        Application.Interfaces.Repositories.IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager)
    {
        _cardPaymentService = cardPaymentService;
        _thirdPartyTransactionService = thirdPartyTransactionService;
        _loanPaymentService = loanPaymentService;
        _depositService = depositService;
        _withdrawalService = withdrawalService;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var teller = await _userManager.GetUserAsync(User);
        var tellerId = teller?.Id ?? Guid.Empty;

        var allTransactions = await _unitOfWork.Transactions.FindAsync(t => t.PerformedById == tellerId);
        var todayTransactions = allTransactions.Where(t => t.Date.Date == today).ToList();

        var depositsCount = todayTransactions.Count(t => t.Origin == "DEPÓSITO" && t.Status == Domain.Enums.TransactionStatus.APROBADA);
        var withdrawalsCount = todayTransactions.Count(t => t.Beneficiary == "RETIRO" && t.Status == Domain.Enums.TransactionStatus.APROBADA);
        var paymentsCount = todayTransactions.Count(t => (t.Origin == "Pago de préstamo" || t.Beneficiary == "Pago de tarjeta") && t.Status == Domain.Enums.TransactionStatus.APROBADA);

        var model = new CashierHomeViewModel
        {
            TotalDepositsToday = depositsCount,
            TotalWithdrawalsToday = withdrawalsCount,
            TotalPaymentsToday = paymentsCount, 
            TotalTransactionsToday = todayTransactions.Count(t => t.Status == Domain.Enums.TransactionStatus.APROBADA)
        };

        return View(model);
    }

    // DEPOSITS 
    [HttpGet]
    public IActionResult Deposit()
    {
        return View(new DepositViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deposit(DepositViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var preview = await _depositService.GetDepositPreviewAsync(model.AccountNumber);

        if (preview == null)
        {
            ModelState.AddModelError("AccountNumber", "El número de cuenta ingresado no corresponde a una cuenta válida.");
            return View(model);
        }

        var confirmModel = new ConfirmDepositViewModel
        {
            AccountNumber = preview.AccountNumber,
            OwnerName = preview.ClientName,
            Amount = model.Amount
        };

        return View("ConfirmDeposit", confirmModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteDeposit(ConfirmDepositViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Deposit));

        var teller = await _userManager.GetUserAsync(User);
        if (teller == null)
        {
            return RedirectToAction(nameof(Deposit));
        }

        var dto = new CreateDepositDto
        {
            AccountNumber = model.AccountNumber,
            Amount = model.Amount
        };

        var result = await _depositService.CreateDepositAsync(teller.Id, dto);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Error ?? "No se pudo procesar el depósito.";
            return RedirectToAction(nameof(Deposit));
        }

        TempData["SuccessMessage"] = result.EmailSent
            ? "El depósito fue realizado correctamente."
            : "El depósito fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
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
    public async Task<IActionResult> Withdrawal(WithdrawalViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var preview = await _withdrawalService.GetWithdrawalPreviewAsync(model.AccountNumber);
        if (preview == null)
        {
            ModelState.AddModelError("AccountNumber", "El número de cuenta ingresado no corresponde a una cuenta válida.");
            return View(model);
        }

        var account = await _unitOfWork.SavingsAccounts.GetByAccountNumberAsync(model.AccountNumber);
        if (account != null && account.Balance < model.Amount)
        {
            ModelState.AddModelError("Amount", "El monto ingresado excede el saldo disponible de la cuenta.");
            return View(model);
        }

        var confirmModel = new ConfirmWithdrawalViewModel
        {
            AccountNumber = preview.AccountNumber,
            OwnerName = preview.ClientName,
            Amount = model.Amount
        };

        return View("ConfirmWithdrawal", confirmModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteWithdrawal(ConfirmWithdrawalViewModel model)
    { 
        if (!ModelState.IsValid) return RedirectToAction(nameof(Withdrawal));

        var teller = await _userManager.GetUserAsync(User);
        if (teller == null)
        {
            return RedirectToAction(nameof(Withdrawal));
        }

        var dto = new CreateWithdrawalDto
        {
            AccountNumber = model.AccountNumber,
            Amount = model.Amount
        };

        var result = await _withdrawalService.CreateWithdrawalAsync(teller.Id, dto);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Error ?? "No se pudo procesar el retiro.";
            return RedirectToAction(nameof(Withdrawal));
        }

        TempData["SuccessMessage"] = result.EmailSent
            ? "El retiro fue realizado correctamente."
            : "El retiro fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
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
    public async Task<IActionResult> PayLoan(PayLoanViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _loanPaymentService.GetLoanPaymentPreviewAsync(model.SourceAccountNumber, model.LoanNumber, model.Amount);

        if (!result.Success)
        {
            var error = result.Error ?? "No se pudo procesar el pago. Verifique los datos ingresados.";
            ModelState.AddModelError(
                error.Contains("cuenta") ? "SourceAccountNumber"
                    : error.Contains("préstamo") ? "LoanNumber"
                    : "Amount",
                error);
            return View(model);
        }

        var preview = result.Preview!;
        var confirmModel = new ConfirmPayLoanViewModel
        {
            SourceAccountOwner = preview.OriginAccountClientName,
            SourceAccountNumber = preview.OriginAccountNumber,
            LoanOwner = preview.LoanClientName,
            LoanNumber = preview.LoanNumber,
            EnteredAmount = preview.EnteredAmount,
            EffectiveAmount = preview.EffectiveAmount
        };

        return View("ConfirmPayLoan", confirmModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecutePayLoan(ConfirmPayLoanViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(PayLoan));

        var teller = await _userManager.GetUserAsync(User);
        if (teller == null)
        {
            return RedirectToAction(nameof(PayLoan));
        }

        var dto = new CreateLoanPaymentDto
        {
            AccountNumber = model.SourceAccountNumber,
            LoanNumber = model.LoanNumber,
            Amount = model.EnteredAmount
        };

        var result = await _loanPaymentService.CreateLoanPaymentAsync(teller.Id, dto);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Error ?? "No se pudo procesar el pago.";
            return RedirectToAction(nameof(PayLoan));
        }

        TempData["SuccessMessage"] = result.EmailSent
            ? "El pago fue realizado correctamente."
            : "El pago fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
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