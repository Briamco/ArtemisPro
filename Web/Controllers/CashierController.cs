using Application.DTOs.Banking;
using Application.Models.ViewModels.Cashier;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

//[Authorize(Roles = "Cajero")]
public class CashierController : Controller
{
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

    private static readonly List<CashierSystemCardDto> _systemCards = new()
    {
        new CashierSystemCardDto { CardNumber = "1234567890123456", FirstName = "Ana", LastName = "Gómez", Status = "Activa", Debt = 2500.00m },
        new CashierSystemCardDto { CardNumber = "9876543210987654", FirstName = "Luis", LastName = "Díaz", Status = "Inactiva", Debt = 1000.00m },
        new CashierSystemCardDto { CardNumber = "1111222233334444", FirstName = "Carlos", LastName = "Ruiz", Status = "Activa", Debt = 0.00m } // Sin deuda
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

        TempData["SuccessMessage"] = "El depósito fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
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

        TempData["SuccessMessage"] = "El retiro fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
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
    public IActionResult PayCreditCard(PayCreditCardViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var account = _systemAccounts.FirstOrDefault(a => a.AccountNumber == model.SourceAccountNumber);
        var card = _systemCards.FirstOrDefault(c => c.CardNumber == model.CreditCardNumber);

        // Validations
        if (account == null || account.Status != "Activa")
        {
            ModelState.AddModelError("SourceAccountNumber", "El número de cuenta ingresado no corresponde a una cuenta válida.");
            return View(model);
        }

        if (card == null || card.Status != "Activa")
        {
            ModelState.AddModelError("CreditCardNumber", "El número de tarjeta ingresado no corresponde a una tarjeta válida.");
            return View(model);
        }

        if (card.Debt <= 0)
        {
            ModelState.AddModelError("CreditCardNumber", "La tarjeta seleccionada no tiene deuda pendiente.");
            return View(model);
        }

        decimal effectiveAmount = Math.Min(model.Amount, card.Debt);

        if (account.Balance < effectiveAmount)
        {
            ModelState.AddModelError("Amount", "El monto ingresado excede el saldo disponible de la cuenta.");
            return View(model);
        }

        var confirmModel = new ConfirmPayCreditCardViewModel
        {
            SourceAccountOwner = $"{account.FirstName} {account.LastName}",
            SourceAccountNumber = account.AccountNumber,
            CreditCardOwner = $"{card.FirstName} {card.LastName}",
            CreditCardMasked = $"**** {card.CardNumber.Substring(12)}",
            EnteredAmount = model.Amount,
            EffectiveAmount = effectiveAmount
        };

        return View("ConfirmPayCreditCard", confirmModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExecutePayCreditCard(ConfirmPayCreditCardViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(PayCreditCard));

        var account = _systemAccounts.FirstOrDefault(a => a.AccountNumber == model.SourceAccountNumber);
        var card = _systemCards.FirstOrDefault(c => model.CreditCardMasked.EndsWith(c.CardNumber.Substring(Math.Max(0, c.CardNumber.Length - 4))));

        if (account == null || account.Status != "Activa" || card == null || card.Status != "Activa" || card.Debt <= 0)
        {
            TempData["ErrorMessage"] = "No se pudo procesar el pago. La cuenta o tarjeta no son válidas o no tienen deuda.";
            return RedirectToAction(nameof(PayCreditCard));
        }

        decimal effectiveAmount = Math.Min(model.EnteredAmount, card.Debt);
        if (effectiveAmount <= 0 || account.Balance < effectiveAmount)
        {
            TempData["ErrorMessage"] = "El saldo de la cuenta no es suficiente para cubrir el monto del pago.";
            return RedirectToAction(nameof(PayCreditCard));
        }

        TempData["SuccessMessage"] = "El pago fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
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

        TempData["SuccessMessage"] = "El pago fue realizado correctamente, pero no fue posible enviar el correo de notificación.";   
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
    public IActionResult ThirdPartyTransfer(ThirdPartyTransferViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var sourceAccount = _systemAccounts.FirstOrDefault(a => a.AccountNumber == model.SourceAccountNumber);
        var destAccount = _systemAccounts.FirstOrDefault(a => a.AccountNumber == model.DestinationAccountNumber);

        // Validations
        if (sourceAccount == null || sourceAccount.Status != "Activa")
        {
            ModelState.AddModelError("SourceAccountNumber", "El número de cuenta origen ingresado no corresponde a una cuenta válida.");
            return View(model);
        }

        if (destAccount == null || destAccount.Status != "Activa")
        {
            ModelState.AddModelError("DestinationAccountNumber", "El número de cuenta destino ingresado no corresponde a una cuenta válida.");
            return View(model);
        }

        if (model.SourceAccountNumber == model.DestinationAccountNumber)
        {
            ModelState.AddModelError("DestinationAccountNumber", "La cuenta origen y la cuenta destino no pueden ser la misma.");
            return View(model);
        }

        if (sourceAccount.Balance < model.Amount)
        {
            ModelState.AddModelError("Amount", "El monto ingresado excede el saldo disponible de la cuenta.");
            return View(model);
        }

        var confirmModel = new ConfirmThirdPartyTransferViewModel
        {
            SourceAccountOwner = $"{sourceAccount.FirstName} {sourceAccount.LastName}",
            SourceAccountNumber = sourceAccount.AccountNumber,
            DestinationAccountOwner = $"{destAccount.FirstName} {destAccount.LastName}",
            DestinationAccountNumber = destAccount.AccountNumber,
            Amount = model.Amount
        };

        return View("ConfirmThirdPartyTransfer", confirmModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExecuteThirdPartyTransfer(ConfirmThirdPartyTransferViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(ThirdPartyTransfer));

        var sourceAccount = _systemAccounts.FirstOrDefault(a => a.AccountNumber == model.SourceAccountNumber);
        var destAccount = _systemAccounts.FirstOrDefault(a => a.AccountNumber == model.DestinationAccountNumber);

        if (sourceAccount == null || sourceAccount.Status != "Activa" ||
            destAccount == null || destAccount.Status != "Activa" ||
            model.SourceAccountNumber == model.DestinationAccountNumber ||
            model.Amount <= 0 || sourceAccount.Balance < model.Amount)
        {
            TempData["ErrorMessage"] = "No se pudo realizar la transferencia. Verifique los datos de las cuentas y el saldo.";
            return RedirectToAction(nameof(ThirdPartyTransfer));
        }

        TempData["SuccessMessage"] = "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.";
        return RedirectToAction("Index"); 
    }
}