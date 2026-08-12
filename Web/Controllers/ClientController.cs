using Application.Models.ViewModels.Client;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

// [Authorize(Roles = "Cliente")] 
public class ClientController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        // simulation
        var rawAccounts = new List<ClientAccountViewModel>
        {
            new ClientAccountViewModel { Id = "acc2", AccountNumber = "987654321", Balance = 15000.00m, IsPrincipal = false },
            new ClientAccountViewModel { Id = "acc1", AccountNumber = "102938475", Balance = 5400.50m, IsPrincipal = true },
            new ClientAccountViewModel { Id = "acc3", AccountNumber = "564738291", Balance = 2000.00m, IsPrincipal = false }
        };

        var loans = new List<ClientLoanViewModel>
        {
            new ClientLoanViewModel { Id = "loan1", LoanNumber = "PR-2026-8942", ApprovedAmount = 150000m, TotalInstallments = 36, PaidInstallments = 9, PendingAmount = 114205.10m, InterestRate = 8.5m, TermInMonths = 36, IsInMora = false }
        };

        var cards = new List<ClientCardViewModel>
        {
            new ClientCardViewModel { Id = "card1", MaskedNumber = "**** **** **** 4921", CreditLimit = 50000m, ExpirationDate = "12/28", DebtAmount = 4500.25m }
        };

        var sortedAccounts = rawAccounts
            .OrderByDescending(a => a.IsPrincipal)
            .ThenByDescending(a => a.Balance)
            .ToList();

        var model = new ClientHomeViewModel
        {
            Accounts = sortedAccounts,
            Loans = loans,
            Cards = cards
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult AccountDetails(string id)
    {
        var model = new ClientAccountDetailsViewModel
        {
            AccountNumber = "102938475",
            CurrentBalance = 5400.50m,
            IsPrincipal = true,
            Transactions = new List<TransactionViewModel>
            {
                new TransactionViewModel { Date = DateTime.Now.AddHours(-2), Amount = 1500m, Type = "DÉBITO", Origin = "102938475", Beneficiary = "987654321", Status = "APROBADA" },
                new TransactionViewModel { Date = DateTime.Now.AddDays(-1), Amount = 200m, Type = "CRÉDITO", Origin = "DEPÓSITO", Beneficiary = "102938475", Status = "APROBADA" }
            }
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult LoanDetails(string id)
    {
        var model = new ClientLoanDetailsViewModel
        {
            LoanNumber = "PR-2026-8942",
            PendingAmount = 114205.10m,
            AmortizationTable = new List<AmortizationViewModel>
            {
                new AmortizationViewModel { DueDate = DateTime.Now.AddMonths(-1), InstallmentValue = 4735.25m, Status = "Pagada", IsOverdue = false },
                new AmortizationViewModel { DueDate = DateTime.Now, InstallmentValue = 4735.25m, Status = "Pendiente", IsOverdue = false }
            }
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult CardDetails(string id)
    {
        var model = new ClientCardDetailsViewModel
        {
            MaskedNumber = "**** **** **** 4921",
            DebtAmount = 4500.25m,
            Consumptions = new List<ConsumptionViewModel>
            {
                new ConsumptionViewModel { Date = DateTime.Now.AddDays(-2), Amount = 1299.00m, Commerce = "Apple Store VIRTUAL", Status = "APROBADO" },
                new ConsumptionViewModel { Date = DateTime.Now.AddDays(-5), Amount = 300.00m, Commerce = "AVANCE", Status = "APROBADO" }
            }
        };
        return View(model);
    }

    // --- beneficiary module ---

    // Simulations
    private static readonly List<SystemAccountDto> _systemAccounts = new()
    {
        new SystemAccountDto { AccountNumber = "102938475", FirstName = "Carlos", LastName = "Mendoza", Status = "Activa", OwnerId = "CURRENT_USER" }, // Cuenta propia
        new SystemAccountDto { AccountNumber = "111222333", FirstName = "Juan", LastName = "Pérez", Status = "Activa", OwnerId = "OTHER_USER" }, // Cuenta válida
        new SystemAccountDto { AccountNumber = "444555666", FirstName = "María", LastName = "López", Status = "Cancelada", OwnerId = "OTHER_USER" }, // Cuenta cancelada
        new SystemAccountDto { AccountNumber = "777888999", FirstName = "Pedro", LastName = "Martínez", Status = "Activa", OwnerId = "OTHER_USER" } // Cuenta válida (Ya agregada)
    };

    private static List<BeneficiaryViewModel> _myBeneficiaries = new()
    {
        new BeneficiaryViewModel { Id = "b1", FirstName = "Pedro", LastName = "Martínez", AccountNumber = "777888999" }
    };

    [HttpGet]
    public IActionResult Beneficiaries()
    {
        var model = new BeneficiaryListViewModel
        {
            Beneficiaries = _myBeneficiaries.ToList()
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult AddBeneficiary()
    {
        return View(new AddBeneficiaryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddBeneficiary(AddBeneficiaryViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var account = _systemAccounts.FirstOrDefault(a => a.AccountNumber == model.AccountNumber);

        // validations
        if (account == null)
        {
            ModelState.AddModelError("AccountNumber", "El número de cuenta ingresado no corresponde a una cuenta válida.");
            return View(model);
        }

        if (account.Status == "Cancelada")
        {
            ModelState.AddModelError("AccountNumber", "No puede agregar una cuenta cancelada como beneficiario.");
            return View(model);
        }

        if (account.OwnerId == "CURRENT_USER")
        {
            ModelState.AddModelError("AccountNumber", "No puede agregar una cuenta propia como beneficiario. Utilice la opción Transferencia para mover fondos entre sus cuentas.");
            return View(model);
        }

        if (_myBeneficiaries.Any(b => b.AccountNumber == model.AccountNumber))
        {
            ModelState.AddModelError("AccountNumber", "Esta cuenta ya se encuentra registrada como beneficiario.");
            return View(model);
        }

        _myBeneficiaries.Add(new BeneficiaryViewModel
        {
            Id = Guid.NewGuid().ToString(),
            FirstName = account.FirstName,
            LastName = account.LastName,
            AccountNumber = account.AccountNumber
        });

        TempData["SuccessMessage"] = "Beneficiario agregado correctamente.";
        return RedirectToAction(nameof(Beneficiaries));
    }

    [HttpGet]
    public IActionResult DeleteBeneficiary(string id)
    {
        var beneficiary = _myBeneficiaries.FirstOrDefault(b => b.Id == id);
        if (beneficiary == null) return RedirectToAction(nameof(Beneficiaries));

        return View(new DeleteBeneficiaryViewModel
        {
            Id = beneficiary.Id,
            FirstName = beneficiary.FirstName,
            LastName = beneficiary.LastName,
            AccountNumber = beneficiary.AccountNumber
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteBeneficiaryConfirmed(string id)
    {
        var beneficiary = _myBeneficiaries.FirstOrDefault(b => b.Id == id);
        if (beneficiary != null)
        {
            _myBeneficiaries.Remove(beneficiary);
            TempData["SuccessMessage"] = "Beneficiario eliminado correctamente.";
        }
        
        return RedirectToAction(nameof(Beneficiaries));
    }

    // express transaction
    [HttpGet]
    public IActionResult TransactionExpress()
    {
        var model = new TransactionExpressViewModel
        {
            MyActiveAccounts = new List<ClientAccountViewModel> { new ClientAccountViewModel { Id = "acc1", AccountNumber = "102938475", Balance = 15000.50m } }
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TransactionExpress(TransactionExpressViewModel model)
    {
        // list of active accounts for simulation
        model.MyActiveAccounts = new List<ClientAccountViewModel> { new ClientAccountViewModel { Id = "acc1", AccountNumber = "102938475", Balance = 15000.50m } };

        if (!ModelState.IsValid) return View(model);

        var sourceAcc = model.MyActiveAccounts.FirstOrDefault(a => a.Id == model.SourceAccountId);
        if (sourceAcc == null) return View(model);

        // validations
        if (model.DestinationAccountNumber == "000000000") 
        {
            ModelState.AddModelError("DestinationAccountNumber", "El número de cuenta ingresado no corresponde a una cuenta válida.");
            return View(model);
        }
        if (model.DestinationAccountNumber == sourceAcc.AccountNumber)
        {
            ModelState.AddModelError("DestinationAccountNumber", "La cuenta destino no puede ser la misma cuenta de origen.");
            return View(model);
        }

        if (sourceAcc.Balance < model.Amount)
        {
            ModelState.AddModelError("Amount", "El monto ingresado excede el saldo disponible de la cuenta seleccionada.");
            return View(model);
        }

        var confirmModel = new ConfirmTransactionExpressViewModel
        {
            SourceAccountId = model.SourceAccountId,
            DestinationAccountNumber = model.DestinationAccountNumber,
            DestinationOwnerName = "Usuario Destino Simulado", 
            Amount = model.Amount
        };

        return View("ConfirmTransactionExpress", confirmModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExecuteTransactionExpress(ConfirmTransactionExpressViewModel model)
    {
        TempData["SuccessMessage"] = "¡Transacción Aprobada! El dinero ha sido enviado correctamente.";
        return RedirectToAction(nameof(TransactionExpress)); 
    }

    //pay credit card
    [HttpGet]
    public IActionResult PayCreditCard()
    {
        var model = new PayCreditCardViewModel
        {
            MyActiveAccounts = new List<ClientAccountViewModel> { new ClientAccountViewModel { Id = "acc1", AccountNumber = "102938475", Balance = 15000.50m } },
            MyActiveCards = new List<ClientCardViewModel> { new ClientCardViewModel { Id = "card1", MaskedNumber = "**** 4921", DebtAmount = 4500.00m } }
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PayCreditCard(PayCreditCardViewModel model)
    {
        model.MyActiveAccounts = new List<ClientAccountViewModel> { new ClientAccountViewModel { Id = "acc1", AccountNumber = "102938475", Balance = 15000.50m } };
        model.MyActiveCards = new List<ClientCardViewModel> { new ClientCardViewModel { Id = "card1", MaskedNumber = "**** 4921", DebtAmount = 4500.00m } };

        if (!ModelState.IsValid) return View(model);

        var sourceAcc = model.MyActiveAccounts.FirstOrDefault(a => a.Id == model.SourceAccountId);
        var destCard = model.MyActiveCards.FirstOrDefault(c => c.Id == model.CreditCardId);

        if (sourceAcc == null || destCard == null) return View(model);

        if (destCard.DebtAmount <= 0)
        {
            ModelState.AddModelError("CreditCardId", "La tarjeta seleccionada no tiene deuda pendiente.");
            return View(model);
        }

        decimal effectiveAmount = Math.Min(model.Amount, destCard.DebtAmount);

        if (sourceAcc.Balance < effectiveAmount)
        {
            ModelState.AddModelError("Amount", "No dispone del monto requerido en la cuenta seleccionada.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"¡Pago Aprobado! Se ha procesado correctamente tu pago de {effectiveAmount:C} a la tarjeta.";
        return RedirectToAction("PayCreditCard"); 
    }

    // loan payment 
    [HttpGet]
    public IActionResult PayLoan()
    {
        var model = new PayLoanViewModel
        {
            MyActiveAccounts = new List<ClientAccountViewModel> { new ClientAccountViewModel { Id = "acc1", AccountNumber = "102938475", Balance = 15000.50m } },
            MyActiveLoans = new List<ClientLoanViewModel> { new ClientLoanViewModel { Id = "loan1", LoanNumber = "PR-2026-8942", PendingAmount = 114205.10m } }
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PayLoan(PayLoanViewModel model)
    {
        model.MyActiveAccounts = new List<ClientAccountViewModel> { new ClientAccountViewModel { Id = "acc1", AccountNumber = "102938475", Balance = 15000.50m } };
        model.MyActiveLoans = new List<ClientLoanViewModel> { new ClientLoanViewModel { Id = "loan1", LoanNumber = "PR-2026-8942", PendingAmount = 114205.10m } };

        if (!ModelState.IsValid) return View(model);

        var sourceAcc = model.MyActiveAccounts.FirstOrDefault(a => a.Id == model.SourceAccountId);
        var destLoan = model.MyActiveLoans.FirstOrDefault(l => l.Id == model.LoanId);

        if (sourceAcc == null || destLoan == null) return View(model);

        if (destLoan.PendingAmount <= 0)
        {
            ModelState.AddModelError("LoanId", "El préstamo seleccionado no tiene cuotas pendientes de pago.");
            return View(model);
        }

        decimal effectiveAmount = Math.Min(model.Amount, destLoan.PendingAmount);

        if (sourceAcc.Balance < effectiveAmount)
        {
            ModelState.AddModelError("Amount", "No dispone del monto requerido en la cuenta seleccionada.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"¡Abono Aprobado! Se aplicó el pago de {effectiveAmount:C} a las cuotas de tu préstamo.";
        return RedirectToAction("PayLoan"); 
    }

    // beneficiary transaction
    [HttpGet]
    public IActionResult TransactionBeneficiary()
    {
        var model = new TransactionBeneficiaryViewModel
        {
            MyActiveAccounts = new List<ClientAccountViewModel> { new ClientAccountViewModel { Id = "acc1", AccountNumber = "102938475", Balance = 15000.50m } },
            MyBeneficiaries = new List<BeneficiaryViewModel> { new BeneficiaryViewModel { Id = "ben1", FirstName = "Pedro", LastName = "Martínez", AccountNumber = "777888999" } }
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TransactionBeneficiary(TransactionBeneficiaryViewModel model)
    {
        model.MyActiveAccounts = new List<ClientAccountViewModel> { new ClientAccountViewModel { Id = "acc1", AccountNumber = "102938475", Balance = 15000.50m } };
        model.MyBeneficiaries = new List<BeneficiaryViewModel> { new BeneficiaryViewModel { Id = "ben1", FirstName = "Pedro", LastName = "Martínez", AccountNumber = "777888999" } };

        if (!ModelState.IsValid) return View(model);

        if (!model.MyBeneficiaries.Any())
        {
            TempData["ErrorMessage"] = "No tiene beneficiarios registrados.";
            return View(model);
        }

        var sourceAcc = model.MyActiveAccounts.FirstOrDefault(a => a.Id == model.SourceAccountId);
        var beneficiary = model.MyBeneficiaries.FirstOrDefault(b => b.Id == model.BeneficiaryId);

        if (sourceAcc == null || beneficiary == null) return View(model);

        if (sourceAcc.Balance < model.Amount)
        {
            ModelState.AddModelError("Amount", "No dispone de fondos suficientes para realizar esta transacción.");
            return View(model);
        }

        var confirmModel = new ConfirmTransactionBeneficiaryViewModel
        {
            SourceAccountId = model.SourceAccountId,
            DestinationAccountNumber = beneficiary.AccountNumber,
            DestinationOwnerName = $"{beneficiary.FirstName} {beneficiary.LastName}",
            Amount = model.Amount
        };

        return View("ConfirmTransactionBeneficiary", confirmModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExecuteTransactionBeneficiary(ConfirmTransactionBeneficiaryViewModel model)
    {
        TempData["SuccessMessage"] = "¡Transacción Aprobada! Los fondos fueron transferidos al beneficiario exitosamente.";
        return RedirectToAction("TransactionBeneficiary"); 
    }
} 
