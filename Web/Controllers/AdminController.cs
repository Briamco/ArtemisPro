using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Application.Models.ViewModels.Admin;
using Application.Interfaces.Services;
using Application.DTOs.Banking;
using Domain.Entities;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    private readonly Application.Interfaces.Services.ICreditCardAppService _creditCardService;
    private readonly Application.Interfaces.Services.IUserAppService _userService;
    private readonly ILoanAppService _loanAppService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISavingsAccountAppService _savingsAccountAppService;

    public AdminController(
        Application.Interfaces.Services.ICreditCardAppService creditCardService, 
        Application.Interfaces.Services.IUserAppService userService,
        ILoanAppService loanAppService, 
        UserManager<ApplicationUser> userManager,
        ISavingsAccountAppService savingsAccountAppService)
    {
        _creditCardService = creditCardService;
        _userService = userService;
        _loanAppService = loanAppService;
        _userManager = userManager;
        _savingsAccountAppService = savingsAccountAppService;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _loanAppService.GetLoansAsync(1, 100, null, null);
        var loans = result.Data.ToList();
        
        var allClients = await _userManager.GetUsersInRoleAsync("Cliente");
        int activeClients = allClients.Count(u => u.IsActive);
        int inactiveClients = allClients.Count(u => !u.IsActive);
        int activeLoans = loans.Count(l => l.Status == "Activo");
        int activeCards = _dummyCards.Count(c => c.Status == "Activa");
        var allSavings = await _savingsAccountAppService.GetSavingsAccountsAsync();
        int activeSavings = allSavings.Count(s => s.Status == "Activa");
        int totalProducts = activeLoans + activeCards + activeSavings;

        decimal totalLoanDebt = loans.Where(l => l.Status == "Activo").Sum(l => l.PendingAmount);
        decimal totalCardDebt = _dummyCards.Where(c => c.Status == "Activa").Sum(c => c.DebtAmount);
        int totalClientsForAvg = activeClients > 0 ? activeClients : 1;
        decimal avgDebt = Math.Round((totalLoanDebt + totalCardDebt) / totalClientsForAvg, 2);

        var dashboardData = new AdminDashboardViewModel
        {
            TotalHistoricalTransactions = 1250,
            DailyTransactions = 42,
            TotalHistoricalPayments = 840,
            DailyPayments = 18,
            ActiveClients = activeClients,
            InactiveClients = inactiveClients,
            AverageDebtPerClient = avgDebt,
            TotalFinancialProducts = totalProducts,
            ActiveLoans = activeLoans,
            ActiveCreditCards = activeCards,
            ActiveSavingsAccounts = activeSavings
        };

        return View(dashboardData);
    }

   //temporary list 
    private static List<UserViewModel> _dummyUsers = new List<UserViewModel>
    {
        new UserViewModel { Id = "1", Username = "jdoe88", Identification = "1234567890", FirstName = "John", LastName = "Doe", Email = "john.doe@email.com", Role = "Administrador", IsActive = true },
        new UserViewModel { Id = "2", Username = "mgarcia", Identification = "0987654321", FirstName = "Maria", LastName = "Garcia", Email = "m.garcia@email.com", Role = "Cajero", IsActive = true },
        new UserViewModel { Id = "3", Username = "asmith", Identification = "4561237890", FirstName = "Alice", LastName = "Smith", Email = "alice.s@email.com", Role = "Cliente", IsActive = false }
    };

    [HttpGet]
    public IActionResult UserManagement(string roleFilter = "Todos", int page = 1)
    {
        var query = _dummyUsers.AsQueryable();

        if (roleFilter != "Todos")
        {
            query = query.Where(u => u.Role == roleFilter);
        }

        var users = query.ToList();
        var model = new UserListViewModel
        {
            Users = users,
            CurrentFilter = roleFilter,
            CurrentPage = page,
            TotalPages = 1, 
            TotalRecords = users.Count
        };

        return View(model);
    }


    //create user 
    [HttpGet]
    public IActionResult CreateUser()
    {
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateUser(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var newUser = new UserViewModel
        {
            Id = Guid.NewGuid().ToString(),
            FirstName = model.FirstName,
            LastName = model.LastName,
            Identification = model.Identification,
            Email = model.Email,
            Username = model.Username,
            Role = model.Role,
            IsActive = true 
        };
  
        _dummyUsers.Insert(0, newUser);
        TempData["SuccessMessage"] = "Usuario creado exitosamente.";

        return RedirectToAction(nameof(UserManagement));
    }

    //edit user
    [HttpGet]
    public IActionResult EditUser(string id)
    {
        var user = _dummyUsers.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "El usuario seleccionado no existe.";
            return RedirectToAction(nameof(UserManagement));
        }

        var currentUsername = User.Identity?.Name ?? "jdoe88"; 
        if (currentUsername == user.Username)
        {
            TempData["ErrorMessage"] = "No puede editar su propia cuenta desde este módulo.";
            return RedirectToAction(nameof(UserManagement));
        }

        var model = new EditUserViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Identification = user.Identification,
            Email = user.Email,
            Username = user.Username,
            Role = user.Role
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditUser(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = _dummyUsers.FirstOrDefault(u => u.Id == model.Id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "El usuario seleccionado no existe.";
            return RedirectToAction(nameof(UserManagement));
        }

        var currentUsername = User.Identity?.Name ?? "jdoe88"; 
        if (currentUsername == user.Username)
        {
            TempData["ErrorMessage"] = "No puede editar su propia cuenta desde este módulo.";
            return RedirectToAction(nameof(UserManagement));
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Identification = model.Identification;
        user.Email = model.Email;
        user.Username = model.Username;
        
        TempData["SuccessMessage"] = "Usuario actualizado exitosamente.";
        return RedirectToAction(nameof(UserManagement));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleUserStatus(string id)
    {
        var user = _dummyUsers.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "El usuario seleccionado no existe.";
            return RedirectToAction(nameof(UserManagement));
        }

        var currentUsername = User.Identity?.Name ?? "jdoe88";
        if (currentUsername == user.Username)
        {
            TempData["ErrorMessage"] = "No puede cambiar el estado de su propia cuenta.";
            return RedirectToAction(nameof(UserManagement));
        }

        user.IsActive = !user.IsActive;
        TempData["SuccessMessage"] = user.IsActive 
            ? "Usuario activado exitosamente." 
            : "Usuario inactivado exitosamente.";

        return RedirectToAction(nameof(UserManagement));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ActivateUser(string id)
    {
        return ToggleUserStatus(id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult InactivateUser(string id)
    {
        return ToggleUserStatus(id);
    }

    [HttpGet]
    public async Task<IActionResult> LoanManagement(string statusFilter = "Activos", string searchCedula = "", int page = 1)
    {
        var apiStatus = statusFilter == "Activos" ? "activos" : statusFilter == "Completados" ? "completados" : "todos";
        var result = await _loanAppService.GetLoansAsync(page, 20, apiStatus, string.IsNullOrEmpty(searchCedula) ? null : searchCedula);

        var loanList = new List<LoanViewModel>();
        foreach (var l in result.Data)
        {
            var user = await _userManager.FindByIdAsync(l.ClientId.ToString());
            loanList.Add(new LoanViewModel
            {
                Id = l.Id.ToString(),
                LoanNumber = l.LoanNumber,
                ClientName = user != null ? $"{user.FirstName} {user.LastName}" : "Desconocido",
                ClientCedula = user?.Cedula ?? "N/A",
                ApprovedCapital = l.CapitalAmount,
                TotalInstallments = l.TotalInstallments,
                PaidInstallments = l.PaidInstallments,
                PendingAmount = l.PendingAmount,
                InterestRate = l.AnnualInterestRate,
                TermInMonths = l.TermInMonths,
                LoanStatus = l.Status,
                ClientStatus = l.ClientPaymentStatus
            });
        }

        var query = loanList.AsQueryable();

        if (!string.IsNullOrEmpty(searchCedula))
        {
            query = query.Where(l => l.ClientCedula.Contains(searchCedula));
            if (!query.Any())
            {
                ViewBag.SearchMessage = "No existe un cliente registrado con esta cédula o este cliente no tiene préstamos registrados.";
            }
        }

        query = query.OrderBy(l => l.LoanStatus == "Completado" ? 1 : 0)
                     .ThenByDescending(l => l.Id);

        var filteredLoans = query.ToList();

        var model = new LoanListViewModel
        {
            Loans = filteredLoans,
            CurrentFilter = statusFilter,
            SearchCedula = searchCedula,
            CurrentPage = result.Page,
            TotalPages = result.TotalPages,
            TotalRecords = result.TotalRecords
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AssignLoanStep1(string searchCedula = "")
    {
        var allClients = await _userManager.GetUsersInRoleAsync("Cliente");
        var activeClients = allClients.Where(c => c.IsActive).ToList();

        if (!string.IsNullOrWhiteSpace(searchCedula))
        {
            var cleanSearch = searchCedula.Trim().Replace("-", "");
            activeClients = activeClients.Where(c => 
                c.Cedula.Contains(searchCedula.Trim(), StringComparison.OrdinalIgnoreCase) || 
                c.Cedula.Replace("-", "").Contains(cleanSearch, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var (averageDebt, _) = await _loanAppService.GetAverageDebtAsync();
        var allLoansResult = await _loanAppService.GetLoansAsync(1, 100, "todos", null);
        var allLoans = allLoansResult.Data.ToList();

        var clients = new List<ClientSelectionViewModel>();
        foreach (var c in activeClients)
        {
            clients.Add(new ClientSelectionViewModel
            {
                Id = c.Id.ToString(),
                Cedula = c.Cedula,
                FullName = $"{c.FirstName} {c.LastName}",
                Email = c.Email ?? string.Empty,
                TotalDebt = allLoans.Where(l => l.ClientId.ToString() == c.Id.ToString() && l.Status == "Activo").Sum(l => l.PendingAmount)
            });
        }

        var model = new AssignLoanStep1ViewModel
        {
            AverageSystemDebt = averageDebt,
            SearchCedula = searchCedula,
            EligibleClients = clients
        };

        return View(model);
    }

[HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AssignLoanStep1(AssignLoanStep1ViewModel model)
    {
        if (string.IsNullOrEmpty(model.SelectedClientId))
        {
            TempData["ErrorMessage"] = "Debe seleccionar un cliente para continuar.";
            return RedirectToAction(nameof(AssignLoanStep1));
        }

        return RedirectToAction(nameof(AssignLoanStep2), new { clientId = model.SelectedClientId });
    }

    [HttpGet]
    public async Task<IActionResult> AssignLoanStep2(string clientId)
    {
        if (!Guid.TryParse(clientId, out var clientGuid))
        {
            TempData["ErrorMessage"] = "ID de cliente inválido.";
            return RedirectToAction(nameof(AssignLoanStep1));
        }

        var client = await _userManager.FindByIdAsync(clientId);
        if (client == null)
        {
            TempData["ErrorMessage"] = "Cliente no encontrado.";
            return RedirectToAction(nameof(AssignLoanStep1));
        }

        var model = new AssignLoanStep2ViewModel
        {
            ClientId = clientId,
            ClientName = $"{client.FirstName} {client.LastName}",
            ClientCedula = client.Cedula
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignLoanStep2(AssignLoanStep2ViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (!Guid.TryParse(model.ClientId, out var clientGuid))
        {
            TempData["ErrorMessage"] = "ID de cliente inválido.";
            return RedirectToAction(nameof(AssignLoanStep1));
        }

        var adminUser = await _userManager.GetUserAsync(User);
        if (adminUser == null)
        {
            TempData["ErrorMessage"] = "No se pudo identificar el administrador actual.";
            return RedirectToAction(nameof(AssignLoanStep1));
        }
        var adminId = adminUser.Id;

        var dto = new CreateLoanDto
        {
            ClientId = clientGuid,
            CapitalAmount = model.Amount,
            AnnualInterestRate = model.InterestRate,
            TermInMonths = model.TermInMonths,
            ConfirmHighRisk = false
        };

        try
        {
            await _loanAppService.CreateLoanAsync(dto, adminId);
        }
        catch (Application.Services.HighRiskConflictException ex)
        {
            var riskModel = new RiskAlertViewModel
            {
                ClientId = model.ClientId,
                CurrentDebt = ex.CurrentDebt,
                ProjectedDebt = ex.ProjectedDebt,
                SystemAverage = ex.AverageDebt,
                Amount = model.Amount,
                InterestRate = model.InterestRate,
                TermInMonths = model.TermInMonths,
                WarningMessage = ex.Message
            };
            return View("RiskAlert", riskModel);
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return View(model);
        }

        TempData["SuccessMessage"] = "Préstamo asignado y desembolsado correctamente.";
        return RedirectToAction(nameof(LoanManagement));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmRiskLoan(RiskAlertViewModel model)
    {
        if (!ModelState.IsValid)
            return View("RiskAlert", model);

        if (!Guid.TryParse(model.ClientId, out var clientGuid))
        {
            TempData["ErrorMessage"] = "ID de cliente inválido.";
            return RedirectToAction(nameof(LoanManagement));
        }

        var adminUser = await _userManager.GetUserAsync(User);
        if (adminUser == null)
        {
            TempData["ErrorMessage"] = "No se pudo identificar el administrador actual.";
            return RedirectToAction(nameof(LoanManagement));
        }
        var adminId = adminUser.Id;

        var dto = new CreateLoanDto
        {
            ClientId = clientGuid,
            CapitalAmount = model.Amount,
            AnnualInterestRate = model.InterestRate,
            TermInMonths = model.TermInMonths,
            ConfirmHighRisk = true
        };

        try
        {
            await _loanAppService.CreateLoanAsync(dto, adminId);
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(LoanManagement));
        }

        TempData["SuccessMessage"] = "Préstamo de alto riesgo asignado y desembolsado bajo su autorización.";
        return RedirectToAction(nameof(LoanManagement));
    }

    [HttpGet]
    public async Task<IActionResult> LoanDetails(Guid id)
    {
        var loan = await _loanAppService.GetLoanByIdAsync(id);
        if (loan == null)
        {
            TempData["ErrorMessage"] = "El préstamo seleccionado no existe.";
            return RedirectToAction(nameof(LoanManagement));
        }

        var model = new LoanDetailsViewModel
        {
            LoanNumber = loan.LoanNumber,
            ClientName = loan.ClientFullName,
            ApprovedAmount = loan.CapitalAmount,
            InterestRate = loan.AnnualInterestRate,
            TermInMonths = loan.TermInMonths,
            LoanStatus = loan.Status,
            PendingBalance = loan.PendingAmount,
            MonthlyQuote = loan.MonthlyInstallment,
            StartDate = loan.CreatedAt,
            NextDueDate = DateTime.Now.AddMonths(1),
            PaymentProgress = 0,
            AmortizationTable = loan.Amortization.Select(i => new AmortizationRowViewModel {
                InstallmentNumber = i.InstallmentNumber,
                DueDate = i.DueDate,
                InstallmentValue = i.InstallmentAmount,
                InterestAmount = i.InterestAmount,
                CapitalAmount = i.CapitalAmount,
                PendingBalance = i.PendingInstallmentAmount,
                PaymentStatus = i.PaymentStatus,
                IsOverdue = i.IsLate
            }).ToList()
        };

        return View(model);
    }


    [HttpGet]
    public async Task<IActionResult> EditLoanRate(Guid id)
    {
        var loan = await _loanAppService.GetLoanByIdAsync(id);
        if (loan == null)
        {
            TempData["ErrorMessage"] = "El préstamo seleccionado no existe.";
            return RedirectToAction(nameof(LoanManagement));
        }

        if (loan.Status != "Activo")
        {
            TempData["ErrorMessage"] = "Solo se puede modificar la tasa de interés de préstamos activos.";
            return RedirectToAction(nameof(LoanManagement));
        }

        var model = new EditLoanRateViewModel
        {
            Id = loan.Id.ToString(),
            InterestRate = loan.AnnualInterestRate
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLoanRate(EditLoanRateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
            
        if (!Guid.TryParse(model.Id, out var loanId))
        {
            TempData["ErrorMessage"] = "ID de préstamo inválido.";
            return RedirectToAction(nameof(LoanManagement));
        }

        var dto = new UpdateLoanRateDto { AnnualInterestRate = model.InterestRate };
        var (success, error) = await _loanAppService.UpdateLoanRateAsync(loanId, dto);
        
        if (!success)
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(LoanManagement));
        }

        TempData["SuccessMessage"] = "Tasa de interés actualizada y cuotas futuras recalculadas correctamente.";
        return RedirectToAction(nameof(LoanManagement));
    }

    // CREDIT CARD MANAGMENT

    private static List<CreditCardViewModel> _dummyCards = new List<CreditCardViewModel>
    {
        new CreditCardViewModel { Id = "1", MaskedNumber = "**** **** **** 4921", ClientName = "Carlos Mendoza", ClientCedula = "092-3456781-9", CreditLimit = 5000.00m, ExpirationDate = "12/26", DebtAmount = 1250.00m, Status = "Activa" },
        new CreditCardViewModel { Id = "2", MaskedNumber = "**** **** **** 8832", ClientName = "Ana Solís", ClientCedula = "175-8392011-4", CreditLimit = 2500.00m, ExpirationDate = "08/24", DebtAmount = 0.00m, Status = "Cancelada" },
        new CreditCardViewModel { Id = "3", MaskedNumber = "**** **** **** 1094", ClientName = "Roberto García", ClientCedula = "050-1234567-8", CreditLimit = 10000.00m, ExpirationDate = "11/27", DebtAmount = 3420.50m, Status = "Activa" }
    };

    [HttpGet]
    public async Task<IActionResult> CreditCardManagement(string statusFilter = "Activas", string searchCedula = "", int page = 1)
    {
        var allCards = await _creditCardService.GetCreditCardsAsync(statusFilter == "Activas" ? "Activa" : (statusFilter == "Canceladas" ? "Cancelada" : null), searchCedula);
        
        int pageSize = 20;
        var paginatedCards = allCards.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        
        var totalActive = allCards.Count(c => c.Status == "Activa");
        var totalDebt = allCards.Sum(c => c.Debt);
        
        var model = new CreditCardListViewModel
        {
            Cards = paginatedCards.Select(c => new CreditCardViewModel 
            {
                Id = c.Id.ToString(),
                MaskedNumber = c.MaskedCardNumber,
                ClientName = c.ClientName,
                ClientCedula = "", 
                CreditLimit = c.Limit,
                ExpirationDate = c.ExpirationDate,
                DebtAmount = c.Debt,
                Status = c.Status
            }).ToList(),
            CurrentFilter = statusFilter,
            SearchCedula = searchCedula,
            TotalActiveCards = totalActive,
            TotalAccumulatedDebt = totalDebt,
            PortfolioRisk = "Bajo",
            CurrentPage = page, 
            TotalPages = Math.Max(1, (int)Math.Ceiling(allCards.Count() / (double)pageSize)), 
            TotalRecords = allCards.Count()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AssignCardStep1(string searchCedula = "")
    {
        var users = await _userService.GetAllUsersAsync("Cliente");
        var query = users.AsQueryable();

        if (!string.IsNullOrEmpty(searchCedula))
        {
            query = query.Where(c => c.Cedula.Contains(searchCedula));
            if (!query.Any()) ViewBag.SearchMessage = "No existe un cliente registrado con esta cédula.";
        }

        var model = new AssignCardStep1ViewModel
        {
            AverageSystemDebt = 4250.00m,
            SearchCedula = searchCedula,
            EligibleClients = query.Select(u => new ClientSelectionViewModel
            {
                Id = u.Id.ToString(),
                Cedula = u.Cedula,
                FullName = $"{u.FirstName} {u.LastName}",
                Email = u.Email,
                TotalDebt = 0m
            }).ToList()
        };
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AssignCardStep1(AssignCardStep1ViewModel model)
    {
        if (string.IsNullOrEmpty(model.SelectedClientId))
        {
            TempData["ErrorMessage"] = "Debe seleccionar un cliente para continuar.";
            return RedirectToAction(nameof(AssignCardStep1));
        }
        return RedirectToAction(nameof(AssignCardStep2), new { clientId = model.SelectedClientId });
    }

    [HttpGet]
    public async Task<IActionResult> AssignCardStep2(string clientId)
    {
        if(!Guid.TryParse(clientId, out var gId)) return RedirectToAction(nameof(CreditCardManagement));
        var user = await _userService.GetUserByIdAsync(gId);
        if(user == null) return RedirectToAction(nameof(CreditCardManagement));

        var model = new AssignCardStep2ViewModel { ClientId = clientId, ClientName = $"{user.FirstName} {user.LastName}", ClientCedula = user.Cedula };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignCardStep2(AssignCardStep2ViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        
        var dto = new Application.DTOs.Banking.AssignCreditCardDto
        {
            ClientId = Guid.Parse(model.ClientId),
            Limit = model.CreditLimit
        };

        var result = await _creditCardService.AssignCreditCardAsync(dto);
        if(result.Success)
        {
            TempData["SuccessMessage"] = $"Tarjeta de crédito asignada exitosamente.";
            return RedirectToAction(nameof(CreditCardManagement));
        }
        else
        {
            TempData["ErrorMessage"] = result.Error;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> CreditCardDetails(string id)
    {
        if(!Guid.TryParse(id, out var gId)) return RedirectToAction(nameof(CreditCardManagement));
        var card = await _creditCardService.GetCreditCardByIdAsync(gId);
        if (card == null)
        {
            TempData["ErrorMessage"] = "La tarjeta seleccionada no existe.";
            return RedirectToAction(nameof(CreditCardManagement));
        }

        var txs = await _creditCardService.GetTransactionsAsync(gId);

        var model = new CreditCardDetailsViewModel
        {
            MaskedNumber = card.MaskedCardNumber,
            ClientName = card.ClientName,
            ExpirationDate = card.ExpirationDate,
            Consumptions = txs.Select(t => new ConsumptionViewModel
            {
                Date = t.Date,
                Commerce = t.MerchantName,
                Amount = t.Amount,
                Status = t.Status
            }).ToList()
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditCreditCardLimit(string id)
    {
        if(!Guid.TryParse(id, out var gId)) return RedirectToAction(nameof(CreditCardManagement));
        var card = await _creditCardService.GetCreditCardByIdAsync(gId);
        if (card == null) { TempData["ErrorMessage"] = "La tarjeta seleccionada no existe."; return RedirectToAction(nameof(CreditCardManagement)); }
        if (card.Status != "Activa") { TempData["ErrorMessage"] = "No se puede modificar una tarjeta cancelada."; return RedirectToAction(nameof(CreditCardManagement)); }

        var model = new EditCreditCardLimitViewModel { Id = card.Id.ToString(), MaskedNumber = card.MaskedCardNumber, CurrentDebt = card.Debt, CurrentLimit = card.Limit, NewLimit = card.Limit };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCreditCardLimit(EditCreditCardLimitViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if(!Guid.TryParse(model.Id, out var gId)) return RedirectToAction(nameof(CreditCardManagement));

        var dto = new Application.DTOs.Banking.UpdateCreditCardLimitDto { NewLimit = model.NewLimit };
        var result = await _creditCardService.UpdateCreditCardLimitAsync(gId, dto);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Límite actualizado correctamente.";
            return RedirectToAction(nameof(CreditCardManagement));
        }
        else
        {
            TempData["ErrorMessage"] = result.Error;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> CancelCreditCard(string id)
    {
        if(!Guid.TryParse(id, out var gId)) return RedirectToAction(nameof(CreditCardManagement));
        var card = await _creditCardService.GetCreditCardByIdAsync(gId);
        if (card == null)
        {
            TempData["ErrorMessage"] = "La tarjeta seleccionada no existe.";
            return RedirectToAction(nameof(CreditCardManagement));
        }
        
        return View(new CancelCreditCardViewModel { Id = card.Id.ToString(), MaskedNumber = card.MaskedCardNumber, CurrentDebt = card.Debt });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelCreditCardConfirmed(string id)
    {
        if(!Guid.TryParse(id, out var gId)) return RedirectToAction(nameof(CreditCardManagement));
        var result = await _creditCardService.CancelCreditCardAsync(gId);
        
        if(result.Success)
        {
            TempData["SuccessMessage"] = "Tarjeta cancelada exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error;
        }
        return RedirectToAction(nameof(CreditCardManagement));
    }

    // ACCOUNT MANAGEMENT

    [HttpGet]
    public async Task<IActionResult> SavingsAccountManagement(string statusFilter = "Activas", string typeFilter = "Todas", string searchCedula = "", int page = 1)
    {
        var domainStatus = statusFilter == "Activas" ? "Activa" : statusFilter == "Canceladas" ? "Cancelada" : null;
        var domainType = typeFilter == "Principal" ? "Principal" : typeFilter == "Secundaria" ? "Secundaria" : null;

        var allAccounts = await _savingsAccountAppService.GetSavingsAccountsAsync(domainStatus, domainType, searchCedula);
        
        var query = allAccounts.AsQueryable();

        if (!string.IsNullOrEmpty(searchCedula) && !query.Any())
        {
            ViewBag.SearchMessage = "No existe un cliente registrado con esta cédula o este cliente no tiene cuentas de ahorro registradas.";
        }

        query = query.OrderBy(a => a.Status.ToString() == "Cancelada" ? 1 : 0).ThenByDescending(a => a.CreatedAt);

        const int pageSize = 20;
        var totalRecords = query.Count();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));
        var accountsDto = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var accounts = accountsDto.Select(a => new SavingsAccountViewModel
        {
            Id = a.Id.ToString(),
            AccountNumber = a.AccountNumber,
            ClientName = a.ClientName,
            ClientCedula = "", // TODO: Map Cedula if needed
            Balance = a.Balance,
            AccountType = a.AccountType.ToString(),
            Status = a.Status.ToString()
        }).ToList();

        var model = new SavingsAccountListViewModel
        {
            Accounts = accounts,
            CurrentStatusFilter = statusFilter,
            CurrentTypeFilter = typeFilter,
            SearchCedula = searchCedula,
            CurrentPage = page, TotalPages = totalPages, TotalRecords = totalRecords
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AssignSavingsAccountStep1(string searchCedula = "")
    {
        var allClients = await _userManager.GetUsersInRoleAsync("Cliente");
        var activeClients = allClients.Where(c => c.IsActive).ToList();

        if (!string.IsNullOrWhiteSpace(searchCedula))
        {
            var cleanSearch = searchCedula.Trim().Replace("-", "");
            activeClients = activeClients.Where(c => 
                c.Cedula.Contains(searchCedula.Trim(), StringComparison.OrdinalIgnoreCase) || 
                c.Cedula.Replace("-", "").Contains(cleanSearch, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var eligibleClients = new List<ClientSelectionViewModel>();
        foreach (var c in activeClients)
        {
            eligibleClients.Add(new ClientSelectionViewModel
            {
                Id = c.Id.ToString(),
                Cedula = c.Cedula,
                FullName = $"{c.FirstName} {c.LastName}",
                Email = c.Email ?? string.Empty,
                TotalDebt = 0 
            });
        }

        if (!string.IsNullOrEmpty(searchCedula) && !eligibleClients.Any())
        {
            ViewBag.SearchMessage = "No existe un cliente registrado con esta cédula.";
        }

        return View(new AssignSavingsAccountStep1ViewModel { SearchCedula = searchCedula, EligibleClients = eligibleClients });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSavingsAccountStep1(AssignSavingsAccountStep1ViewModel model)
    {
        if (string.IsNullOrEmpty(model.SelectedClientId))
        {
            TempData["ErrorMessage"] = "Debe seleccionar un cliente para continuar.";
            return RedirectToAction(nameof(AssignSavingsAccountStep1));
        }

        if (!Guid.TryParse(model.SelectedClientId, out var clientGuid))
        {
            TempData["ErrorMessage"] = "ID de cliente inválido.";
            return RedirectToAction(nameof(AssignSavingsAccountStep1));
        }

        var clientAccounts = await _savingsAccountAppService.GetSavingsAccountsAsync();
        var hasPrincipalAccount = clientAccounts.Any(a => a.ClientId == clientGuid && a.AccountType == "Principal" && a.Status == "Activa");

        if (!hasPrincipalAccount)
        {
            TempData["ErrorMessage"] = "El cliente debe tener una cuenta de ahorro principal activa antes de asignarle una cuenta secundaria.";
            return RedirectToAction(nameof(AssignSavingsAccountStep1));
        }

        return RedirectToAction(nameof(AssignSavingsAccountStep2), new { clientId = model.SelectedClientId });
    }

    [HttpGet]
    public async Task<IActionResult> AssignSavingsAccountStep2(string clientId)
    {
        if (!Guid.TryParse(clientId, out var gId)) return RedirectToAction(nameof(AssignSavingsAccountStep1));
        var client = await _userManager.FindByIdAsync(clientId);
        if (client == null) return RedirectToAction(nameof(AssignSavingsAccountStep1));

        var model = new AssignSavingsAccountStep2ViewModel 
        { 
            ClientId = clientId, 
            ClientName = $"{client.FirstName} {client.LastName}", 
            ClientCedula = client.Cedula 
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSavingsAccountStep2(AssignSavingsAccountStep2ViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        
        var dto = new CreateSavingsAccountDto 
        { 
            ClientId = Guid.Parse(model.ClientId), 
            InitialBalance = model.InitialBalance 
        };
        var result = await _savingsAccountAppService.CreateSavingsAccountAsync(dto);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Error;
            return View(model);
        }

        TempData["SuccessMessage"] = $"Cuenta de ahorro secundaria asignada correctamente.{(model.InitialBalance > 0 ? " Transacción de CRÉDITO inicial registrada." : "")}";
        return RedirectToAction(nameof(SavingsAccountManagement));
    }

    [HttpGet]
    public async Task<IActionResult> SavingsAccountDetails(string id)
    {
        if (!Guid.TryParse(id, out var gId)) return RedirectToAction(nameof(SavingsAccountManagement));
        var account = await _savingsAccountAppService.GetSavingsAccountByIdAsync(gId);
        if (account == null)
        {
            TempData["ErrorMessage"] = "La cuenta seleccionada no existe.";
            return RedirectToAction(nameof(SavingsAccountManagement));
        }

        var txs = await _savingsAccountAppService.GetTransactionsAsync(gId);

        var model = new SavingsAccountDetailsViewModel
        {
            AccountNumber = account.AccountNumber,
            ClientName = account.ClientName,
            CurrentBalance = account.Balance,
            AccountType = account.AccountType,
            Transactions = txs.OrderByDescending(t => t.Date).Select(t => new TransactionViewModel
            {
                Date = t.Date,
                Amount = t.Amount,
                Type = t.Type,
                Beneficiary = t.Beneficiary,
                Origin = t.Origin,
                Status = t.Status
            }).ToList()
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CancelSavingsAccount(string id)
    {
        if (!Guid.TryParse(id, out var gId)) return RedirectToAction(nameof(SavingsAccountManagement));
        var account = await _savingsAccountAppService.GetSavingsAccountByIdAsync(gId);
        
        if (account == null) { TempData["ErrorMessage"] = "La cuenta seleccionada no existe."; return RedirectToAction(nameof(SavingsAccountManagement)); }
        if (account.AccountType == "Principal") { TempData["ErrorMessage"] = "Las cuentas principales no pueden ser canceladas."; return RedirectToAction(nameof(SavingsAccountManagement)); }
        
        return View(new CancelSavingsAccountViewModel { Id = account.Id.ToString(), AccountNumber = account.AccountNumber, CurrentBalance = account.Balance });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelSavingsAccountConfirmed(string id)
    {
        if (!Guid.TryParse(id, out var gId)) return RedirectToAction(nameof(SavingsAccountManagement));
        
        var account = await _savingsAccountAppService.GetSavingsAccountByIdAsync(gId);
        if (account == null)
        {
            TempData["ErrorMessage"] = "La cuenta seleccionada no existe.";
            return RedirectToAction(nameof(SavingsAccountManagement));
        }

        if (account.AccountType == "Principal")
        {
            TempData["ErrorMessage"] = "Las cuentas principales no pueden ser canceladas.";
            return RedirectToAction(nameof(SavingsAccountManagement));
        }

        decimal balance = account.Balance;
        var result = await _savingsAccountAppService.CancelSavingsAccountAsync(gId);
        
        if (result.Success)
        {
            if (balance > 0)
            {
                TempData["SuccessMessage"] = $"Cuenta secundaria cancelada exitosamente. El balance de {balance:C} fue transferido a la cuenta principal del cliente.";
            }
            else
            {
                TempData["SuccessMessage"] = "Cuenta secundaria cancelada exitosamente.";
            }
        }
        else
        {
            TempData["ErrorMessage"] = result.Error;
        }

        return RedirectToAction(nameof(SavingsAccountManagement));
    }
}