using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Services;
using Application.Models.ViewModels.Client;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Web.Helpers;

namespace Web.Controllers;

[Authorize(Roles = "Cliente")] 
public class ClientController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISavingsAccountAppService _savingsAccountService;
    private readonly ILoanAppService _loanService;
    private readonly ICreditCardAppService _creditCardService;
    private readonly IBeneficiaryAppService _beneficiaryService;
    private readonly ITransferAppService _transferService;
    private readonly IPaymentAppService _paymentService;
    private readonly IThirdPartyTransactionAppService _thirdPartyTransactionService;

    public ClientController(
        UserManager<ApplicationUser> userManager,
        ISavingsAccountAppService savingsAccountService,
        ILoanAppService loanService,
        ICreditCardAppService creditCardService,
        IBeneficiaryAppService beneficiaryService,
        ITransferAppService transferService,
        IPaymentAppService paymentService,
        IThirdPartyTransactionAppService thirdPartyTransactionService)
    {
        _userManager = userManager;
        _savingsAccountService = savingsAccountService;
        _loanService = loanService;
        _creditCardService = creditCardService;
        _beneficiaryService = beneficiaryService;
        _transferService = transferService;
        _paymentService = paymentService;
        _thirdPartyTransactionService = thirdPartyTransactionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var accounts = await _savingsAccountService.GetClientAccountsAsync(user.Id);
        var activeAccounts = accounts
            .Where(a => a.Status == AccountStatus.Activa.ToString())
            .OrderByDescending(a => a.AccountType == AccountType.Principal.ToString())
            .ThenByDescending(a => a.Balance)
            .Select(a => new ClientAccountViewModel
            {
                Id = a.Id.ToString(),
                AccountNumber = a.AccountNumber,
                Balance = a.Balance,
                IsPrincipal = a.AccountType == AccountType.Principal.ToString()
            })
            .ToList();

        var loans = await _loanService.GetClientLoansAsync(user.Id);
        var activeLoans = new List<ClientLoanViewModel>();
        foreach (var l in loans.Where(l => l.Status == LoanStatus.Activo.ToString()))
        {
            var installments = await _loanService.GetInstallmentsAsync(l.Id);
            var paidCount = installments.Count(i => i.PaymentStatus == PaymentStatus.Pagada.ToString());
            var pendingDebt = installments.Sum(i => i.PendingBalance);
            var isMora = installments.Any(i => i.IsOverdue);

            activeLoans.Add(new ClientLoanViewModel
            {
                Id = l.Id.ToString(),
                LoanNumber = l.LoanNumber,
                PendingAmount = pendingDebt,
                ApprovedAmount = l.ApprovedAmount,
                TotalInstallments = l.Term,
                PaidInstallments = paidCount,
                InterestRate = l.AnnualInterestRate,
                TermInMonths = l.Term,
                IsInMora = isMora
            });
        }

        var cards = await _creditCardService.GetClientCardsAsync(user.Id);
        var activeCards = cards
            .Where(c => c.Status == CardStatus.Activa.ToString())
            .Select(c => new ClientCardViewModel
            {
                Id = c.Id.ToString(),
                MaskedNumber = TransactionHelpers.FormatCardMask(c.MaskedCardNumber),
                CreditLimit = c.Limit,
                DebtAmount = c.Debt,
                ExpirationDate = c.ExpirationDate
            })
            .ToList();

        var model = new ClientHomeViewModel
        {
            Accounts = activeAccounts,
            Loans = activeLoans,
            Cards = activeCards
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AccountDetails(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var accountGuid))
            return RedirectToAction(nameof(Index));

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var account = await _savingsAccountService.GetSavingsAccountByIdAsync(accountGuid);
        if (account == null || account.ClientId != user.Id)
            return RedirectToAction(nameof(Index));

        var rawTransactions = await _savingsAccountService.GetTransactionsAsync(accountGuid);
        var transactions = rawTransactions
            .OrderByDescending(t => t.Date)
            .Select(t => new TransactionViewModel
            {
                Date = t.Date,
                Amount = t.Amount,
                Type = t.Type.ToString(),
                Origin = TransactionHelpers.ResolveTransactionOrigin(t.Origin),
                Beneficiary = TransactionHelpers.ResolveTransactionBeneficiary(t.Beneficiary),
                Status = t.Status.ToString()
            })
            .ToList();

        var model = new ClientAccountDetailsViewModel
        {
            AccountNumber = account.AccountNumber,
            CurrentBalance = account.Balance,
            IsPrincipal = account.AccountType == AccountType.Principal.ToString(),
            Transactions = transactions
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> LoanDetails(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var loanGuid))
            return RedirectToAction(nameof(Index));

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var loan = await _loanService.GetLoanByIdAsync(loanGuid);
        if (loan == null || loan.ClientId != user.Id)
            return RedirectToAction(nameof(Index));

        var rawInstallments = await _loanService.GetInstallmentsAsync(loanGuid);
        var installments = rawInstallments
            .OrderBy(i => i.InstallmentNumber)
            .Select(i => new AmortizationViewModel
            {
                DueDate = i.DueDate,
                InstallmentValue = i.Amount,
                Status = i.PaymentStatus.ToString(),
                IsOverdue = i.IsOverdue
            })
            .ToList();

        var totalPending = rawInstallments.Sum(i => i.PendingBalance);

        var model = new ClientLoanDetailsViewModel
        {
            LoanNumber = loan.LoanNumber,
            PendingAmount = totalPending,
            AmortizationTable = installments
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CardDetails(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var cardGuid))
            return RedirectToAction(nameof(Index));

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var card = await _creditCardService.GetCreditCardByIdAsync(cardGuid);
        if (card == null || card.ClientId != user.Id)
            return RedirectToAction(nameof(Index));

        var rawTransactions = await _creditCardService.GetTransactionsAsync(cardGuid);
        var consumptions = rawTransactions
            .OrderByDescending(t => t.Date)
            .Select(t => new ConsumptionViewModel
            {
                Date = t.Date,
                Amount = t.Amount,
                Commerce = t.MerchantName,
                Status = t.Status
            })
            .ToList();

        var model = new ClientCardDetailsViewModel
        {
            MaskedNumber = TransactionHelpers.FormatCardMask(card.MaskedCardNumber),
            DebtAmount = card.Debt,
            Consumptions = consumptions
        };

        return View(model);
    }

    // --- beneficiary module ---

    [HttpGet]
    public async Task<IActionResult> Beneficiaries()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var beneficiariesDto = await _beneficiaryService.GetByClientIdAsync(user.Id);

        var model = new BeneficiaryListViewModel
        {
            Beneficiaries = beneficiariesDto.Select(b => new BeneficiaryViewModel
            {
                Id = b.Id.ToString(),
                FirstName = b.OwnerFirstName,
                LastName = b.OwnerLastName,
                AccountNumber = b.BeneficiaryAccountNumber
            }).ToList()
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
    public async Task<IActionResult> AddBeneficiary(AddBeneficiaryViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var dto = new CreateBeneficiaryDto
        {
            BeneficiaryAccountNumber = model.AccountNumber
        };

        var (success, error) = await _beneficiaryService.CreateBeneficiaryAsync(user.Id, dto);
        if (!success)
        {
            ModelState.AddModelError("AccountNumber", error ?? "No se pudo agregar el beneficiario.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Beneficiario agregado correctamente.";
        return RedirectToAction(nameof(Beneficiaries));
    }

    [HttpGet]
    public async Task<IActionResult> DeleteBeneficiary(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var beneficiaryGuid))
            return RedirectToAction(nameof(Beneficiaries));

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var beneficiaries = await _beneficiaryService.GetByClientIdAsync(user.Id);
        var beneficiary = beneficiaries.FirstOrDefault(b => b.Id == beneficiaryGuid);
        if (beneficiary == null) return RedirectToAction(nameof(Beneficiaries));

        return View(new DeleteBeneficiaryViewModel
        {
            Id = beneficiary.Id.ToString(),
            FirstName = beneficiary.OwnerFirstName,
            LastName = beneficiary.OwnerLastName,
            AccountNumber = beneficiary.BeneficiaryAccountNumber
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBeneficiaryConfirmed(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var beneficiaryGuid))
            return RedirectToAction(nameof(Beneficiaries));

        var (success, error) = await _beneficiaryService.DeleteBeneficiaryAsync(beneficiaryGuid);
        if (success)
        {
            TempData["SuccessMessage"] = "Beneficiario eliminado correctamente.";
        }
        else
        {
            TempData["ErrorMessage"] = error ?? "No se pudo eliminar el beneficiario.";
        }
        
        return RedirectToAction(nameof(Beneficiaries));
    }

    // express transaction
    [HttpGet]
    public async Task<IActionResult> TransactionExpress()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var model = new TransactionExpressViewModel
        {
            MyActiveAccounts = await GetActiveClientAccountsAsync(user.Id)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransactionExpress(TransactionExpressViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        model.MyActiveAccounts = await GetActiveClientAccountsAsync(user.Id);
        if (!ModelState.IsValid) return View(model);

        var sourceAcc = model.MyActiveAccounts.FirstOrDefault(a => a.Id == model.SourceAccountId);
        if (sourceAcc == null)
        {
            ModelState.AddModelError("SourceAccountId", "La cuenta de origen no es válida.");
            return View(model);
        }

        var preview = await _thirdPartyTransactionService.GetPreviewAsync(sourceAcc.AccountNumber, model.DestinationAccountNumber, model.Amount);
        if (!preview.Success)
        {
            var err = preview.Error ?? "No se pudo validar la transacción.";
            ModelState.AddModelError(
                err.Contains("origen") ? "SourceAccountId"
                    : err.Contains("destino") ? "DestinationAccountNumber"
                    : "Amount",
                err);
            return View(model);
        }

        var confirmModel = new ConfirmTransactionExpressViewModel
        {
            SourceAccountId = model.SourceAccountId,
            DestinationAccountNumber = model.DestinationAccountNumber,
            DestinationOwnerName = preview.Preview!.DestinationAccountOwner, 
            Amount = model.Amount
        };

        return View("ConfirmTransactionExpress", confirmModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteTransactionExpress(ConfirmTransactionExpressViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (!Guid.TryParse(model.SourceAccountId, out var sourceGuid))
        {
            TempData["ErrorMessage"] = "Identificador de cuenta inválido.";
            return RedirectToAction(nameof(TransactionExpress));
        }

        var sourceAcc = await _savingsAccountService.GetSavingsAccountByIdAsync(sourceGuid);
        if (sourceAcc == null)
        {
            TempData["ErrorMessage"] = "Cuenta de origen no encontrada.";
            return RedirectToAction(nameof(TransactionExpress));
        }

        var dto = new CreateThirdPartyTransactionDto
        {
            SourceAccountNumber = sourceAcc.AccountNumber,
            DestinationAccountNumber = model.DestinationAccountNumber,
            Amount = model.Amount
        };

        var result = await _thirdPartyTransactionService.CreateTransactionAsync(user.Id, dto);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.EmailSent
                ? "¡Transacción Aprobada! El dinero ha sido enviado correctamente."
                : "¡Transacción Aprobada! El dinero ha sido enviado, pero ocurrió un error al enviar el correo de notificación.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error ?? "No se pudo procesar la transacción.";
        }
        return RedirectToAction(nameof(TransactionExpress)); 
    }

    //pay credit card
    [HttpGet]
    public async Task<IActionResult> PayCreditCard()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var model = new PayCreditCardViewModel
        {
            MyActiveAccounts = await GetActiveClientAccountsAsync(user.Id),
            MyActiveCards = await GetActiveClientCardsAsync(user.Id)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayCreditCard(PayCreditCardViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        model.MyActiveAccounts = await GetActiveClientAccountsAsync(user.Id);
        model.MyActiveCards = await GetActiveClientCardsAsync(user.Id);

        if (!ModelState.IsValid) return View(model);

        if (!Guid.TryParse(model.SourceAccountId, out var sourceGuid) || !Guid.TryParse(model.CreditCardId, out var cardGuid))
        {
            TempData["ErrorMessage"] = "Datos seleccionados inválidos.";
            return View(model);
        }

        var dto = new PayCreditCardDto
        {
            ClientId = user.Id,
            SourceAccountId = sourceGuid,
            CreditCardId = cardGuid,
            Amount = model.Amount
        };

        var (success, error) = await _paymentService.PayCreditCardAsync(dto);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "No se pudo procesar el pago.";
            return View(model);
        }

        TempData["SuccessMessage"] = $"¡Pago Aprobado! Se ha procesado correctamente el pago a su tarjeta de crédito.";
        return RedirectToAction(nameof(PayCreditCard)); 
    }

    // loan payment 
    [HttpGet]
    public async Task<IActionResult> PayLoan()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var model = new PayLoanViewModel
        {
            MyActiveAccounts = await GetActiveClientAccountsAsync(user.Id),
            MyActiveLoans = await GetActiveClientLoansAsync(user.Id)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayLoan(PayLoanViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        model.MyActiveAccounts = await GetActiveClientAccountsAsync(user.Id);
        model.MyActiveLoans = await GetActiveClientLoansAsync(user.Id);

        if (!ModelState.IsValid) return View(model);

        if (!Guid.TryParse(model.SourceAccountId, out var sourceGuid) || !Guid.TryParse(model.LoanId, out var loanGuid))
        {
            TempData["ErrorMessage"] = "Datos seleccionados inválidos.";
            return View(model);
        }

        var dto = new PayLoanDto
        {
            ClientId = user.Id,
            SourceAccountId = sourceGuid,
            LoanId = loanGuid,
            Amount = model.Amount
        };

        var (success, error) = await _paymentService.PayLoanAsync(dto);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "No se pudo procesar el pago.";
            return View(model);
        }

        TempData["SuccessMessage"] = $"¡Abono Aprobado! Se aplicó el pago a las cuotas de su préstamo.";
        return RedirectToAction(nameof(PayLoan)); 
    }

    // beneficiary transaction
    [HttpGet]
    public async Task<IActionResult> TransactionBeneficiary()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var beneficiaries = await _beneficiaryService.GetByClientIdAsync(user.Id);

        var model = new TransactionBeneficiaryViewModel
        {
            MyActiveAccounts = await GetActiveClientAccountsAsync(user.Id),
            MyBeneficiaries = beneficiaries.Select(b => new BeneficiaryViewModel
            {
                Id = b.Id.ToString(),
                FirstName = b.OwnerFirstName,
                LastName = b.OwnerLastName,
                AccountNumber = b.BeneficiaryAccountNumber
            }).ToList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransactionBeneficiary(TransactionBeneficiaryViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var beneficiaries = await _beneficiaryService.GetByClientIdAsync(user.Id);
        model.MyActiveAccounts = await GetActiveClientAccountsAsync(user.Id);
        model.MyBeneficiaries = beneficiaries.Select(b => new BeneficiaryViewModel
        {
            Id = b.Id.ToString(),
            FirstName = b.OwnerFirstName,
            LastName = b.OwnerLastName,
            AccountNumber = b.BeneficiaryAccountNumber
        }).ToList();

        if (!ModelState.IsValid) return View(model);

        if (!model.MyBeneficiaries.Any())
        {
            TempData["ErrorMessage"] = "No tiene beneficiarios registrados.";
            return View(model);
        }

        var sourceAcc = model.MyActiveAccounts.FirstOrDefault(a => a.Id == model.SourceAccountId);
        var beneficiary = model.MyBeneficiaries.FirstOrDefault(b => b.Id == model.BeneficiaryId);

        if (sourceAcc == null || beneficiary == null)
        {
            TempData["ErrorMessage"] = "Datos de cuenta o beneficiario inválidos.";
            return View(model);
        }

        var preview = await _thirdPartyTransactionService.GetPreviewAsync(sourceAcc.AccountNumber, beneficiary.AccountNumber, model.Amount);
        if (!preview.Success)
        {
            ModelState.AddModelError("Amount", preview.Error ?? "No se pudo realizar la validación.");
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
    public async Task<IActionResult> ExecuteTransactionBeneficiary(ConfirmTransactionBeneficiaryViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (!Guid.TryParse(model.SourceAccountId, out var sourceGuid))
        {
            TempData["ErrorMessage"] = "Identificador de cuenta inválido.";
            return RedirectToAction(nameof(TransactionBeneficiary));
        }

        var sourceAcc = await _savingsAccountService.GetSavingsAccountByIdAsync(sourceGuid);
        if (sourceAcc == null)
        {
            TempData["ErrorMessage"] = "Cuenta de origen no encontrada.";
            return RedirectToAction(nameof(TransactionBeneficiary));
        }

        var dto = new CreateThirdPartyTransactionDto
        {
            SourceAccountNumber = sourceAcc.AccountNumber,
            DestinationAccountNumber = model.DestinationAccountNumber,
            Amount = model.Amount
        };

        var result = await _thirdPartyTransactionService.CreateTransactionAsync(user.Id, dto);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.EmailSent
                ? "¡Transacción Aprobada! Los fondos fueron transferidos al beneficiario exitosamente."
                : "¡Transacción Aprobada! Fondos transferidos, pero ocurrió un error al enviar las notificaciones.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error ?? "No se pudo procesar la transacción.";
        }

        return RedirectToAction(nameof(TransactionBeneficiary)); 
    }

    // cash advance
    [HttpGet]
    public async Task<IActionResult> CashAdvance()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var model = new CashAdvanceViewModel
        {
            MyActiveCards = await GetActiveClientCardsAsync(user.Id),
            MyActiveAccounts = await GetActiveClientAccountsAsync(user.Id)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CashAdvance(CashAdvanceViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        model.MyActiveCards = await GetActiveClientCardsAsync(user.Id);
        model.MyActiveAccounts = await GetActiveClientAccountsAsync(user.Id);

        if (!ModelState.IsValid) return View(model);

        if (!Guid.TryParse(model.CreditCardId, out var cardGuid) || !Guid.TryParse(model.AccountId, out var accGuid))
        {
            TempData["ErrorMessage"] = "Datos seleccionados inválidos.";
            return View(model);
        }

        var dto = new CashAdvanceDto
        {
            ClientId = user.Id,
            CreditCardId = cardGuid,
            DestinationAccountId = accGuid,
            Amount = model.Amount
        };

        var (success, error) = await _paymentService.CashAdvanceAsync(dto);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "No se pudo procesar el avance de efectivo.";
            return View(model);
        }

        TempData["SuccessMessage"] = !string.IsNullOrEmpty(error)
            ? error
            : "¡Avance Aprobado! El avance de efectivo fue realizado correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // transfer between accounts
    [HttpGet]
    public async Task<IActionResult> Transfer()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var model = new TransferViewModel
        {
            MyActiveAccounts = await GetActiveClientAccountsAsync(user.Id)
        };

        if (model.MyActiveAccounts.Count < 2)
        {
            TempData["ErrorMessage"] = "Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas.";
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(TransferViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        model.MyActiveAccounts = await GetActiveClientAccountsAsync(user.Id);

        if (model.MyActiveAccounts.Count < 2)
        {
            TempData["ErrorMessage"] = "Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas.";
            return View(model);
        }

        if (!ModelState.IsValid) return View(model);

        var sourceAcc = model.MyActiveAccounts.FirstOrDefault(a => a.Id == model.SourceAccountId);
        var destAcc = model.MyActiveAccounts.FirstOrDefault(a => a.Id == model.DestinationAccountId);

        if (sourceAcc == null || destAcc == null)
        {
            TempData["ErrorMessage"] = "Cuentas seleccionadas inválidas.";
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
    public async Task<IActionResult> ExecuteTransfer(ConfirmTransferViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (!Guid.TryParse(model.SourceAccountId, out var srcGuid) || !Guid.TryParse(model.DestinationAccountId, out var dstGuid))
        {
            TempData["ErrorMessage"] = "Identificadores de cuenta inválidos.";
            return RedirectToAction(nameof(Transfer));
        }

        var dto = new CreateTransferDto
        {
            OriginAccountId = srcGuid,
            DestinationAccountId = dstGuid,
            Amount = model.Amount
        };

        var result = await _transferService.CreateTransferAsync(user.Id, dto);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.EmailSent
                ? "¡Transferencia Aprobada! La transferencia entre sus cuentas fue realizada correctamente."
                : "La transferencia fue realizada correctamente, pero no fue posible enviar el correo de notificación.";

            return RedirectToAction(nameof(Index));
        }
        else
        {
            TempData["ErrorMessage"] = result.Error ?? "No se pudo procesar la transferencia.";
            return RedirectToAction(nameof(Transfer));
        }
    }

    #region Private Helper Methods
    private async Task<List<ClientAccountViewModel>> GetActiveClientAccountsAsync(Guid clientId)
    {
        var accounts = await _savingsAccountService.GetClientAccountsAsync(clientId);
        return accounts
            .Where(a => a.Status == AccountStatus.Activa.ToString())
            .OrderByDescending(a => a.AccountType == AccountType.Principal.ToString())
            .ThenByDescending(a => a.Balance)
            .Select(a => new ClientAccountViewModel
            {
                Id = a.Id.ToString(),
                AccountNumber = a.AccountNumber,
                Balance = a.Balance,
                IsPrincipal = a.AccountType == AccountType.Principal.ToString()
            })
            .ToList();
    }

    private async Task<List<ClientCardViewModel>> GetActiveClientCardsAsync(Guid clientId)
    {
        var cards = await _creditCardService.GetClientCardsAsync(clientId);
        return cards
            .Where(c => c.Status == CardStatus.Activa.ToString())
            .Select(c => new ClientCardViewModel
            {
                Id = c.Id.ToString(),
                MaskedNumber = TransactionHelpers.FormatCardMask(c.MaskedCardNumber),
                CreditLimit = c.Limit,
                DebtAmount = c.Debt,
                ExpirationDate = c.ExpirationDate
            })
            .ToList();
    }

    private async Task<List<ClientLoanViewModel>> GetActiveClientLoansAsync(Guid clientId)
    {
        var loans = await _loanService.GetClientLoansAsync(clientId);
        var activeLoans = new List<ClientLoanViewModel>();
        foreach (var l in loans.Where(l => l.Status == LoanStatus.Activo.ToString()))
        {
            var installments = await _loanService.GetInstallmentsAsync(l.Id);
            var paidCount = installments.Count(i => i.PaymentStatus == PaymentStatus.Pagada.ToString());
            var pendingDebt = installments.Sum(i => i.PendingBalance);
            var isMora = installments.Any(i => i.IsOverdue);

            activeLoans.Add(new ClientLoanViewModel
            {
                Id = l.Id.ToString(),
                LoanNumber = l.LoanNumber,
                PendingAmount = pendingDebt,
                ApprovedAmount = l.ApprovedAmount,
                TotalInstallments = l.Term,
                PaidInstallments = paidCount,
                InterestRate = l.AnnualInterestRate,
                TermInMonths = l.Term,
                IsInMora = isMora
            });
        }
        return activeLoans;
    }
    #endregion
}
