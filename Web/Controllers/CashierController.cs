using Application.Models.ViewModels.Cashier;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

// [Authorize(Roles = "Cajero")]
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
        new CashierSystemAccountDto { AccountNumber = "444555666", FirstName = "María", LastName = "López", Status = "Inactiva", Balance = 1000.00m }
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
        TempData["SuccessMessage"] = "El retiro fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
        return RedirectToAction("Index"); 
    }


}