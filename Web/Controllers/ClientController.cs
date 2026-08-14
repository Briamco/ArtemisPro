using Application.Models.ViewModels.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Web.Controllers;

[Authorize(Roles = "Cliente")] 
public class ClientController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var rawAccounts = GetActiveAccounts();

        var loans = GetActiveLoans();

        var cards = GetActiveCards();

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
        if (string.IsNullOrWhiteSpace(id))
            return RedirectToAction(nameof(Index));

        var account = GetActiveAccounts().FirstOrDefault(a => a.Id == id);
        if (account == null)
            return RedirectToAction(nameof(Index));

        var model = new ClientAccountDetailsViewModel
        {
            AccountNumber = account.AccountNumber,
            CurrentBalance = account.Balance,
            IsPrincipal = account.IsPrincipal,
            Transactions = new List<TransactionViewModel>
            {
                new TransactionViewModel { Date = DateTime.Now.AddHours(-2), Amount = 1500m, Type = "DÉBITO", Origin = account.AccountNumber, Beneficiary = "987654321", Status = "APROBADA" },
                new TransactionViewModel { Date = DateTime.Now.AddDays(-1), Amount = 200m, Type = "CRÉDITO", Origin = "DEPÓSITO", Beneficiary = account.AccountNumber, Status = "APROBADA" }
            }
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult LoanDetails(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return RedirectToAction(nameof(Index));

        var loan = GetActiveLoans().FirstOrDefault(l => l.Id == id);
        if (loan == null)
            return RedirectToAction(nameof(Index));

        var model = new ClientLoanDetailsViewModel
        {
            LoanNumber = loan.LoanNumber,
            PendingAmount = loan.PendingAmount,
            AmortizationTable = new List<AmortizationViewModel>
            {
                new AmortizationViewModel { DueDate = DateTime.Now.AddMonths(-1), InstallmentValue = 4735.25m, Status = "Pagada", IsOverdue = false },
                new AmortizationViewModel { DueDate = DateTime.Now, InstallmentValue = 4735.25m, Status = "Pendiente", IsOverdue = loan.IsInMora }
            }
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult CardDetails(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return RedirectToAction(nameof(Index));

        var card = GetActiveCards().FirstOrDefault(c => c.Id == id);
        if (card == null)
            return RedirectToAction(nameof(Index));

        var model = new ClientCardDetailsViewModel
        {
            MaskedNumber = card.MaskedNumber,
            DebtAmount = card.DebtAmount,
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
        if (string.IsNullOrWhiteSpace(id)) return RedirectToAction(nameof(Beneficiaries));

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
        if (string.IsNullOrWhiteSpace(id)) return RedirectToAction(nameof(Beneficiaries));

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
            MyActiveAccounts = GetActiveAccounts()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TransactionExpress(TransactionExpressViewModel model)
    {
        model.MyActiveAccounts = GetActiveAccounts();

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
        var sourceAcc = _activeAccounts.FirstOrDefault(a => a.Id == model.SourceAccountId);
        if (sourceAcc != null && sourceAcc.Balance >= model.Amount)
        {
            sourceAcc.Balance -= model.Amount;
            var destAcc = _activeAccounts.FirstOrDefault(a => a.AccountNumber == model.DestinationAccountNumber);
            if (destAcc != null)
            {
                destAcc.Balance += model.Amount;
            }
            TempData["SuccessMessage"] = "¡Transacción Aprobada! El dinero ha sido enviado correctamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se pudo procesar la transacción debido a saldo insuficiente.";
        }
        return RedirectToAction(nameof(TransactionExpress)); 
    }

    //pay credit card
    [HttpGet]
    public IActionResult PayCreditCard()
    {
        var model = new PayCreditCardViewModel
        {
            MyActiveAccounts = GetActiveAccounts(),
            MyActiveCards = GetActiveCards()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PayCreditCard(PayCreditCardViewModel model)
    {
        model.MyActiveAccounts = GetActiveAccounts();
        model.MyActiveCards = GetActiveCards();

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

        sourceAcc.Balance -= effectiveAmount;
        destCard.DebtAmount -= effectiveAmount;

        TempData["SuccessMessage"] = $"¡Pago Aprobado! Se ha procesado correctamente tu pago de {effectiveAmount:C} a la tarjeta.";
        return RedirectToAction(nameof(PayCreditCard)); 
    }

    // loan payment 
    [HttpGet]
    public IActionResult PayLoan()
    {
        var model = new PayLoanViewModel
        {
            MyActiveAccounts = GetActiveAccounts(),
            MyActiveLoans = GetActiveLoans()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PayLoan(PayLoanViewModel model)
    {
        model.MyActiveAccounts = GetActiveAccounts();
        model.MyActiveLoans = GetActiveLoans();

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

        sourceAcc.Balance -= effectiveAmount;
        destLoan.PendingAmount -= effectiveAmount;

        TempData["SuccessMessage"] = $"¡Abono Aprobado! Se aplicó el pago de {effectiveAmount:C} a las cuotas de tu préstamo.";
        return RedirectToAction(nameof(PayLoan)); 
    }

    // beneficiary transaction
    [HttpGet]
    public IActionResult TransactionBeneficiary()
    {
        var model = new TransactionBeneficiaryViewModel
        {
            MyActiveAccounts = GetActiveAccounts(),
            MyBeneficiaries = _myBeneficiaries.ToList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TransactionBeneficiary(TransactionBeneficiaryViewModel model)
    {
        model.MyActiveAccounts = GetActiveAccounts();
        model.MyBeneficiaries = _myBeneficiaries.ToList();

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
        var sourceAcc = _activeAccounts.FirstOrDefault(a => a.Id == model.SourceAccountId);
        if (sourceAcc != null && sourceAcc.Balance >= model.Amount)
        {
            sourceAcc.Balance -= model.Amount;
            var destAcc = _activeAccounts.FirstOrDefault(a => a.AccountNumber == model.DestinationAccountNumber);
            if (destAcc != null)
            {
                destAcc.Balance += model.Amount;
            }
            TempData["SuccessMessage"] = "¡Transacción Aprobada! Los fondos fueron transferidos al beneficiario exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se pudo procesar la transacción debido a saldo insuficiente.";
        }
        return RedirectToAction(nameof(TransactionBeneficiary)); 
    }

    // cash advance
    [HttpGet]
    public IActionResult CashAdvance()
    {
        var model = new CashAdvanceViewModel
        {
            MyActiveCards = GetActiveCards(),
            MyActiveAccounts = GetActiveAccounts()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CashAdvance(CashAdvanceViewModel model)
    {
        model.MyActiveCards = GetActiveCards();
        model.MyActiveAccounts = GetActiveAccounts();

        if (!ModelState.IsValid) return View(model);

        var sourceCard = model.MyActiveCards.FirstOrDefault(c => c.Id == model.CreditCardId);
        var destAccount = model.MyActiveAccounts.FirstOrDefault(a => a.Id == model.AccountId);

        if (sourceCard == null || destAccount == null) return View(model);

        if (model.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "El monto del avance debe ser mayor que cero.");
            return View(model);
        }

        decimal availableCredit = sourceCard.CreditLimit - sourceCard.DebtAmount;
        decimal interest = model.Amount * 0.0625m; 
        decimal totalCharge = model.Amount + interest;

        if (totalCharge > availableCredit)
        {
            ModelState.AddModelError("Amount", "El avance solicitado excede el crédito disponible de la tarjeta seleccionada.");
            return View(model);
        }

        sourceCard.DebtAmount += totalCharge;
        destAccount.Balance += model.Amount;

        TempData["SuccessMessage"] = "¡Avance Aprobado! El avance de efectivo fue realizado correctamente.";
        
        return RedirectToAction(nameof(CashAdvance)); 
    }

    // transfer between accounts
    [HttpGet]
    public IActionResult Transfer()
    {
        var model = new TransferViewModel
        {
            MyActiveAccounts = GetActiveAccounts()
        };

        if (model.MyActiveAccounts.Count < 2)
        {
            TempData["ErrorMessage"] = "Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas.";
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Transfer(TransferViewModel model)
    {
        model.MyActiveAccounts = GetActiveAccounts();

        if (model.MyActiveAccounts.Count < 2)
        {
            TempData["ErrorMessage"] = "Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas.";
            return View(model);
        }

        if (!ModelState.IsValid) return View(model);

        var sourceAcc = model.MyActiveAccounts.FirstOrDefault(a => a.Id == model.SourceAccountId);
        var destAcc = model.MyActiveAccounts.FirstOrDefault(a => a.Id == model.DestinationAccountId);

        if (sourceAcc == null || destAcc == null) return View(model);

        if (sourceAcc.Id == destAcc.Id)
        {
            ModelState.AddModelError("DestinationAccountId", "La cuenta de origen y la cuenta de destino no pueden ser la misma.");
            return View(model);
        }

        if (model.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "El monto a transferir debe ser mayor que cero.");
            return View(model);
        }

        if (sourceAcc.Balance < model.Amount)
        {
            ModelState.AddModelError("Amount", "No dispone del monto requerido en la cuenta seleccionada.");
            return View(model);
        }

        var confirmModel = new ConfirmTransferViewModel
        {
            SourceAccountId = sourceAcc.Id,
            DestinationAccountId = destAcc.Id,
            SourceAccountNumber = sourceAcc.AccountNumber,
            DestinationAccountNumber = destAcc.AccountNumber,
            Amount = model.Amount
        };

        return View("ConfirmTransfer", confirmModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExecuteTransfer(ConfirmTransferViewModel model)
    {
        var sourceAcc = _activeAccounts.FirstOrDefault(a => a.Id == model.SourceAccountId);
        var destAcc = _activeAccounts.FirstOrDefault(a => a.Id == model.DestinationAccountId);

        if (sourceAcc != null && destAcc != null && sourceAcc.Balance >= model.Amount)
        {
            sourceAcc.Balance -= model.Amount;
            destAcc.Balance += model.Amount;
            TempData["SuccessMessage"] = "¡Transferencia Aprobada! La transferencia entre sus cuentas fue realizada correctamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se pudo procesar la transferencia.";
        }
        return RedirectToAction(nameof(Transfer)); 
    }

    #region Helper Methods
    private static readonly List<ClientAccountViewModel> _activeAccounts = new()
    {
        new ClientAccountViewModel { Id = "acc1", AccountNumber = "102938475", Balance = 15000.50m, IsPrincipal = true },
        new ClientAccountViewModel { Id = "acc2", AccountNumber = "987654321", Balance = 2500.00m, IsPrincipal = false }
    };

    private static readonly List<ClientCardViewModel> _activeCards = new()
    {
        new ClientCardViewModel { Id = "card1", MaskedNumber = "**** 4921", CreditLimit = 50000m, DebtAmount = 4500.00m, ExpirationDate = "12/28" }
    };

    private static readonly List<ClientLoanViewModel> _activeLoans = new()
    {
        new ClientLoanViewModel { Id = "loan1", LoanNumber = "PR-2026-8942", PendingAmount = 114205.10m, ApprovedAmount = 150000m, TotalInstallments = 36, PaidInstallments = 9, InterestRate = 8.5m, TermInMonths = 36, IsInMora = false }
    };

    private List<ClientAccountViewModel> GetActiveAccounts() => _activeAccounts;
    private List<ClientCardViewModel> GetActiveCards() => _activeCards;
    private List<ClientLoanViewModel> GetActiveLoans() => _activeLoans;
    #endregion
}
