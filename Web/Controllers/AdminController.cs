using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Models.ViewModels.Admin;
using Application.Interfaces.Services;
using Application.DTOs.Banking;

namespace Web.Controllers;

//[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    private readonly ILoanAppService _loanAppService;

    public AdminController(ILoanAppService loanAppService)
    {
        _loanAppService = loanAppService;
    }

    public async Task<IActionResult> Index()
    {
        var loans = await _loanAppService.GetLoansAsync();
        
        int activeClients = _dummyUsers.Count(u => u.Role == "Cliente" && u.IsActive);
        int inactiveClients = _dummyUsers.Count(u => u.Role == "Cliente" && !u.IsActive);
        int activeLoans = loans.Count(l => l.Status == "Activo");
        int activeCards = _dummyCards.Count(c => c.Status == "Activa");
        int activeSavings = _dummySavings.Count(s => s.Status == "Activa");
        int totalProducts = activeLoans + activeCards + activeSavings;

        decimal totalLoanDebt = loans.Where(l => l.Status == "Activo").Sum(l => l.ApprovedAmount);
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

    // LOAN MODULE



   [HttpGet]
    public async Task<IActionResult> LoanManagement(string statusFilter = "Activos", string searchCedula = "", int page = 1)
    {
        var domainStatus = statusFilter == "Activos" ? "Activo" : statusFilter == "Completados" ? "Completado" : null;
        var loansDto = await _loanAppService.GetLoansAsync(domainStatus, searchCedula);
        
        var loans = loansDto.Select(l => new LoanViewModel
        {
            Id = l.Id.ToString(),
            LoanNumber = l.LoanNumber,
            ClientName = "Cliente", // Ideally fetched from user service
            ClientCedula = searchCedula, // Simplification for UI binding
            ApprovedCapital = l.ApprovedAmount,
            InterestRate = l.AnnualInterestRate,
            TermInMonths = l.Term,
            LoanStatus = l.Status,
            PendingAmount = l.ApprovedAmount // Simple mock since the real one requires installments calculation
        }).ToList();

        var model = new LoanListViewModel
        {
            Loans = loans,
            CurrentFilter = statusFilter,
            SearchCedula = searchCedula,
            CurrentPage = page,
            TotalPages = 1,
            TotalRecords = loans.Count
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult AssignLoanStep1(string searchCedula = "")
    {
        var clients = new List<ClientSelectionViewModel>
        {
            new ClientSelectionViewModel { Id = "c1", Cedula = "402-1234567-8", FullName = "Juan Pérez Domínguez", Email = "juan.perez@email.com", TotalDebt = 0.00m },
            new ClientSelectionViewModel { Id = "c2", Cedula = "001-9876543-2", FullName = "María Rodríguez Alba", Email = "m.rodriguez@empresa.com.do", TotalDebt = 15200.50m },
            new ClientSelectionViewModel { Id = "c3", Cedula = "031-4567890-1", FullName = "Carlos Sánchez Mella", Email = "csanchez88@gmail.com", TotalDebt = 5000.00m }
        };

        if (!string.IsNullOrWhiteSpace(searchCedula))
        {
            var cleanSearch = searchCedula.Trim().Replace("-", "");
            clients = clients.Where(c => c.Cedula.Contains(searchCedula.Trim(), StringComparison.OrdinalIgnoreCase) || 
                                         c.Cedula.Replace("-", "").Contains(cleanSearch, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var model = new AssignLoanStep1ViewModel
        {
            AverageSystemDebt = 125450.00m,
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
    public IActionResult AssignLoanStep2(string clientId)
    {
        var model = new AssignLoanStep2ViewModel
        {
            ClientId = clientId,
            ClientName = "Roberto Sánchez Almánzar",
            ClientCedula = "001-1234567-8"
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AssignLoanStep2(AssignLoanStep2ViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // simulation of risk evaluation
        decimal systemAverage = 120000.00m;
        decimal currentDebt = 45200.00m; 

        //calulation of the projected debt after assigning the new loan
        int n = model.TermInMonths <= 0 ? 1 : model.TermInMonths;
        decimal r = (model.InterestRate / 100) / 12;
        decimal P = model.Amount;
        decimal C = model.InterestRate == 0 ? (P / n) : (P * (r * (decimal)Math.Pow((double)(1 + r), n)) / ((decimal)Math.Pow((double)(1 + r), n) - 1));
        
        // calculation of the total amount to pay and the projected debt
        decimal totalToPay = C * n;
        decimal projectedDebt = currentDebt + totalToPay;

        // risk evaluation
        if (projectedDebt > systemAverage)
        {
            var riskModel = new RiskAlertViewModel
            {
                ClientId = model.ClientId,
                CurrentDebt = currentDebt,
                ProjectedDebt = projectedDebt,
                SystemAverage = systemAverage,
                Amount = model.Amount,
                InterestRate = model.InterestRate,
                TermInMonths = model.TermInMonths,
                WarningMessage = currentDebt > systemAverage 
                    ? "Este cliente se considera de alto riesgo, ya que su deuda actual supera el promedio del sistema."
                    : "Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, ya que su deuda superará el umbral promedio del sistema."
            };
            return View("RiskAlert", riskModel); 
        }

        TempData["SuccessMessage"] = "Préstamo asignado y desembolsado correctamente (Sin riesgo).";
        return RedirectToAction(nameof(LoanManagement));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmRiskLoan(RiskAlertViewModel model)
    {
        if (!ModelState.IsValid)
            return View("RiskAlert", model);

        TempData["SuccessMessage"] = "Préstamo de alto riesgo asignado bajo su autorización.";
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
        
        var installments = await _loanAppService.GetInstallmentsAsync(id);

        var model = new LoanDetailsViewModel
        {
            LoanNumber = loan.LoanNumber,
            ClientName = "Cliente",
            ApprovedAmount = loan.ApprovedAmount,
            InterestRate = loan.AnnualInterestRate,
            TermInMonths = loan.Term,
            LoanStatus = loan.Status,
            PendingBalance = loan.ApprovedAmount,
            MonthlyQuote = loan.ApprovedAmount / loan.Term,
            StartDate = loan.CreatedAt,
            NextDueDate = DateTime.Now.AddMonths(1),
            PaymentProgress = 0,
            AmortizationTable = installments.Select(i => new AmortizationRowViewModel {
                InstallmentNumber = i.InstallmentNumber,
                DueDate = i.DueDate,
                InstallmentValue = i.Amount,
                InterestAmount = i.InterestAmount,
                CapitalAmount = i.CapitalAmount,
                PendingBalance = i.PendingBalance,
                PaymentStatus = i.PaymentStatus,
                IsOverdue = i.IsOverdue
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
            
        var dto = new UpdateLoanRateDto { AnnualInterestRate = model.InterestRate };
        var (success, error) = await _loanAppService.UpdateLoanRateAsync(Guid.Parse(model.Id), dto);
        
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
    public IActionResult CreditCardManagement(string statusFilter = "Activas", string searchCedula = "", int page = 1)
    {
        var query = _dummyCards.AsQueryable();

        if (statusFilter == "Activas") query = query.Where(c => c.Status == "Activa");
        else if (statusFilter == "Canceladas") query = query.Where(c => c.Status == "Cancelada");

        if (!string.IsNullOrEmpty(searchCedula))
        {
            query = query.Where(c => c.ClientCedula.Contains(searchCedula));
            if (!query.Any()) ViewBag.SearchMessage = "No existe un cliente registrado con esta cédula o este cliente no tiene tarjetas registradas.";
        }

        query = query.OrderBy(c => c.Status == "Cancelada" ? 1 : 0).ThenByDescending(c => c.Id);

        var cards = query.ToList();
        var model = new CreditCardListViewModel
        {
            Cards = cards,
            CurrentFilter = statusFilter,
            SearchCedula = searchCedula,
            TotalActiveCards = _dummyCards.Count(c => c.Status == "Activa"),
            TotalAccumulatedDebt = _dummyCards.Sum(c => c.DebtAmount),
            PortfolioRisk = "Bajo",
            CurrentPage = page, TotalPages = 1, TotalRecords = cards.Count
        };

        return View(model);
    }

   [HttpGet]
    public IActionResult AssignCardStep1(string searchCedula = "")
    {
        var allClients = new List<ClientSelectionViewModel>
        {
            new ClientSelectionViewModel { Id = "c1", Cedula = "402-1234567-8", FullName = "Carlos E. Mendoza", Email = "cmendoza@example.com", TotalDebt = 8450.00m },
            new ClientSelectionViewModel { Id = "c2", Cedula = "001-9876543-2", FullName = "Laura V. Castillo", Email = "lcastillo@example.com", TotalDebt = 0.00m },
            new ClientSelectionViewModel { Id = "c3", Cedula = "223-4567890-1", FullName = "Roberto Sánchez", Email = "rsanchez_corp@example.com", TotalDebt = 1200.50m },
            new ClientSelectionViewModel { Id = "c4", Cedula = "031-1122334-4", FullName = "Ana María Rojas", Email = "am_rojas@example.com", TotalDebt = 12300.00m }
        };

        var query = allClients.AsQueryable();

        if (!string.IsNullOrEmpty(searchCedula))
        {
            query = query.Where(c => c.Cedula.Contains(searchCedula));
            
            if (!query.Any())
            {
                ViewBag.SearchMessage = "No existe un cliente registrado con esta cédula.";
            }
        }

        var model = new AssignCardStep1ViewModel
        {
            AverageSystemDebt = 4250.00m,
            SearchCedula = searchCedula,
            EligibleClients = query.ToList()
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
    public IActionResult AssignCardStep2(string clientId)
    {
        var model = new AssignCardStep2ViewModel { ClientId = clientId, ClientName = "Roberto Medina", ClientCedula = "001-1234567-8" };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AssignCardStep2(AssignCardStep2ViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        
        var expDate = DateTime.Now.AddYears(3).ToString("MM/yy");
        
        TempData["SuccessMessage"] = $"Tarjeta de crédito asignada exitosamente. Vencimiento: {expDate}. (Correo enviado al cliente)";
        return RedirectToAction(nameof(CreditCardManagement));
    }

    [HttpGet]
    public IActionResult CreditCardDetails(string id)
    {
        var card = _dummyCards.FirstOrDefault(c => c.Id == id);
        if (card == null)
        {
            TempData["ErrorMessage"] = "La tarjeta seleccionada no existe.";
            return RedirectToAction(nameof(CreditCardManagement));
        }

        var model = new CreditCardDetailsViewModel
        {
            MaskedNumber = card.MaskedNumber,
            ClientName = card.ClientName,
            ExpirationDate = card.ExpirationDate,
            Consumptions = new List<ConsumptionViewModel>
            {
                new ConsumptionViewModel { Date = DateTime.Now.AddDays(-2), Commerce = "Apple Store VIRTUAL", Amount = 1299.00m, Status = "APROBADO" },
                new ConsumptionViewModel { Date = DateTime.Now.AddDays(-4), Commerce = "AVANCE EFECTIVO ATM", Amount = 300.00m, Status = "APROBADO" },
                new ConsumptionViewModel { Date = DateTime.Now.AddDays(-5), Commerce = "Aerolíneas Argentinas", Amount = 850.50m, Status = "RECHAZADO" }
            }
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult EditCreditCardLimit(string id)
    {
        var card = _dummyCards.FirstOrDefault(c => c.Id == id);
        if (card == null) { TempData["ErrorMessage"] = "La tarjeta seleccionada no existe."; return RedirectToAction(nameof(CreditCardManagement)); }
        if (card.Status != "Activa") { TempData["ErrorMessage"] = "No se puede modificar una tarjeta cancelada."; return RedirectToAction(nameof(CreditCardManagement)); }

        var model = new EditCreditCardLimitViewModel { Id = card.Id, MaskedNumber = card.MaskedNumber, CurrentDebt = card.DebtAmount, CurrentLimit = card.CreditLimit, NewLimit = card.CreditLimit };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditCreditCardLimit(EditCreditCardLimitViewModel model)
    {
        var card = _dummyCards.FirstOrDefault(c => c.Id == model.Id);
        if (card == null)
        {
            TempData["ErrorMessage"] = "La tarjeta seleccionada no existe.";
            return RedirectToAction(nameof(CreditCardManagement));
        }

        if (card.Status != "Activa")
        {
            TempData["ErrorMessage"] = "No se puede modificar una tarjeta cancelada.";
            return RedirectToAction(nameof(CreditCardManagement));
        }

        if (model.NewLimit < card.DebtAmount)
        {
            ModelState.AddModelError("NewLimit", "El límite de la tarjeta no puede ser inferior al monto adeudado actualmente.");
            return View(model);
        }

        if (!ModelState.IsValid) return View(model);

        card.CreditLimit = model.NewLimit;
        TempData["SuccessMessage"] = "Límite actualizado correctamente.";
        return RedirectToAction(nameof(CreditCardManagement));
    }

    [HttpGet]
    public IActionResult CancelCreditCard(string id)
    {
        var card = _dummyCards.FirstOrDefault(c => c.Id == id);
        if (card == null)
        {
            TempData["ErrorMessage"] = "La tarjeta seleccionada no existe.";
            return RedirectToAction(nameof(CreditCardManagement));
        }
        
        return View(new CancelCreditCardViewModel { Id = card.Id, MaskedNumber = card.MaskedNumber.Length >= 4 ? card.MaskedNumber.Substring(card.MaskedNumber.Length - 4) : card.MaskedNumber, CurrentDebt = card.DebtAmount });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CancelCreditCardConfirmed(string id)
    {
        var card = _dummyCards.FirstOrDefault(c => c.Id == id);
        if (card == null)
        {
            TempData["ErrorMessage"] = "La tarjeta seleccionada no existe.";
            return RedirectToAction(nameof(CreditCardManagement));
        }

        if (card.DebtAmount > 0)
        {
            TempData["ErrorMessage"] = "Para cancelar esta tarjeta, el cliente debe saldar la totalidad de la deuda pendiente.";
            return RedirectToAction(nameof(CancelCreditCard), new { id });
        }

        card.Status = "Cancelada";
        TempData["SuccessMessage"] = "Tarjeta cancelada exitosamente.";
        return RedirectToAction(nameof(CreditCardManagement));
    }

    // ACCOUNT MANAGEMENT

    private static List<SavingsAccountViewModel> _dummySavings = new List<SavingsAccountViewModel>
    {
        new SavingsAccountViewModel { Id = "1", AccountNumber = "102938475", ClientName = "Carlos Mendoza", ClientCedula = "402-1234567-8", Balance = 15400.50m, AccountType = "Principal", Status = "Activa" },
        new SavingsAccountViewModel { Id = "2", AccountNumber = "987654321", ClientName = "Carlos Mendoza", ClientCedula = "402-1234567-8", Balance = 2500.00m, AccountType = "Secundaria", Status = "Activa" },
        new SavingsAccountViewModel { Id = "3", AccountNumber = "564738291", ClientName = "Laura V. Castillo", ClientCedula = "001-9876543-2", Balance = 0.00m, AccountType = "Secundaria", Status = "Cancelada" }
    };

    [HttpGet]
    public IActionResult SavingsAccountManagement(string statusFilter = "Activas", string typeFilter = "Todas", string searchCedula = "", int page = 1)
    {
        var query = _dummySavings.AsQueryable();
        //FILTERS 
        if (statusFilter == "Activas") query = query.Where(a => a.Status == "Activa");
        else if (statusFilter == "Canceladas") query = query.Where(a => a.Status == "Cancelada");

        if (typeFilter == "Principal") query = query.Where(a => a.AccountType == "Principal");
        else if (typeFilter == "Secundaria") query = query.Where(a => a.AccountType == "Secundaria");

        if (!string.IsNullOrEmpty(searchCedula))
        {
            query = query.Where(a => a.ClientCedula.Contains(searchCedula));
            if (!query.Any()) ViewBag.SearchMessage = "No existe un cliente registrado con esta cédula o este cliente no tiene cuentas de ahorro registradas.";
        }

        query = query.OrderBy(a => a.Status == "Cancelada" ? 1 : 0).ThenByDescending(a => a.Id);

        var accounts = query.ToList();
        var model = new SavingsAccountListViewModel
        {
            Accounts = accounts,
            CurrentStatusFilter = statusFilter,
            CurrentTypeFilter = typeFilter,
            SearchCedula = searchCedula,
            CurrentPage = page, TotalPages = 1, TotalRecords = accounts.Count
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult AssignSavingsAccountStep1(string searchCedula = "")
    {
        var allClients = new List<ClientSelectionViewModel>
        {
            new ClientSelectionViewModel { Id = "c1", Cedula = "402-1234567-8", FullName = "Carlos E. Mendoza", Email = "cmendoza@example.com", TotalDebt = 8450.00m },
            new ClientSelectionViewModel { Id = "c2", Cedula = "001-9876543-2", FullName = "Laura V. Castillo", Email = "lcastillo@example.com", TotalDebt = 0.00m }
        };

        var query = allClients.AsQueryable();
        if (!string.IsNullOrEmpty(searchCedula))
        {
            query = query.Where(c => c.Cedula.Contains(searchCedula));
            if (!query.Any()) ViewBag.SearchMessage = "No existe un cliente registrado con esta cédula.";
        }

        return View(new AssignSavingsAccountStep1ViewModel { SearchCedula = searchCedula, EligibleClients = query.ToList() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AssignSavingsAccountStep1(AssignSavingsAccountStep1ViewModel model)
    {
        if (string.IsNullOrEmpty(model.SelectedClientId))
        {
            TempData["ErrorMessage"] = "Debe seleccionar un cliente para continuar.";
            return RedirectToAction(nameof(AssignSavingsAccountStep1));
        }

        //simulation 
        bool hasPrincipalAccount = true;
        if (!hasPrincipalAccount)
        {
            TempData["ErrorMessage"] = "El cliente debe tener una cuenta de ahorro principal activa antes de asignarle una cuenta secundaria.";
            return RedirectToAction(nameof(AssignSavingsAccountStep1));
        }

        return RedirectToAction(nameof(AssignSavingsAccountStep2), new { clientId = model.SelectedClientId });
    }

    [HttpGet]
    public IActionResult AssignSavingsAccountStep2(string clientId)
    {
        var model = new AssignSavingsAccountStep2ViewModel { ClientId = clientId, ClientName = "Carlos E. Mendoza", ClientCedula = "402-1234567-8" };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AssignSavingsAccountStep2(AssignSavingsAccountStep2ViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        
        TempData["SuccessMessage"] = $"Cuenta de ahorro secundaria asignada correctamente.{(model.InitialBalance > 0 ? " Transacción de CRÉDITO inicial registrada." : "")}";
        return RedirectToAction(nameof(SavingsAccountManagement));
    }

    [HttpGet]
    public IActionResult SavingsAccountDetails(string id)
    {
        var account = _dummySavings.FirstOrDefault(a => a.Id == id);
        if (account == null)
        {
            TempData["ErrorMessage"] = "La cuenta seleccionada no existe.";
            return RedirectToAction(nameof(SavingsAccountManagement));
        }

        var model = new SavingsAccountDetailsViewModel
        {
            AccountNumber = account.AccountNumber,
            ClientName = account.ClientName,
            CurrentBalance = account.Balance,
            AccountType = account.AccountType,
            Transactions = new List<TransactionViewModel>
            {
                new TransactionViewModel { Date = DateTime.Now.AddDays(-1), Amount = 500.00m, Type = "CRÉDITO", Beneficiary = account.AccountNumber, Origin = "DEPÓSITO", Status = "APROBADA" },
                new TransactionViewModel { Date = DateTime.Now.AddDays(-3), Amount = 1200.00m, Type = "DÉBITO", Beneficiary = "RETIRO", Origin = account.AccountNumber, Status = "APROBADA" },
                new TransactionViewModel { Date = DateTime.Now.AddDays(-5), Amount = 5000.00m, Type = "DÉBITO", Beneficiary = "8924", Origin = account.AccountNumber, Status = "RECHAZADA" }
            }
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult CancelSavingsAccount(string id)
    {
        var account = _dummySavings.FirstOrDefault(a => a.Id == id);
        if (account == null) { TempData["ErrorMessage"] = "La cuenta seleccionada no existe."; return RedirectToAction(nameof(SavingsAccountManagement)); }
        if (account.AccountType == "Principal") { TempData["ErrorMessage"] = "Las cuentas principales no pueden ser canceladas."; return RedirectToAction(nameof(SavingsAccountManagement)); }
        
        return View(new CancelSavingsAccountViewModel { Id = account.Id, AccountNumber = account.AccountNumber, CurrentBalance = account.Balance });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CancelSavingsAccountConfirmed(string id)
    {
        var account = _dummySavings.FirstOrDefault(a => a.Id == id);
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

        account.Status = "Cancelada";
        
        if (account.Balance > 0)
        {
            TempData["SuccessMessage"] = $"Cuenta secundaria cancelada exitosamente. El balance de {account.Balance:C} fue transferido a la cuenta principal del cliente.";
        }
        else
        {
            TempData["SuccessMessage"] = "Cuenta secundaria cancelada exitosamente.";
        }

        return RedirectToAction(nameof(SavingsAccountManagement));
    }
}