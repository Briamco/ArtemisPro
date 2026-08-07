using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Models.ViewModels.Admin;

namespace Web.Controllers;

//[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    public IActionResult Index()
    {
        // to be replaced with real data from database services
        var dashboardData = new AdminDashboardViewModel
        {
            TotalHistoricalTransactions = 0,
            DailyTransactions = 0,
            TotalHistoricalPayments = 0,
            DailyPayments = 0,
            ActiveClients = 0,
            InactiveClients = 0,
            AverageDebtPerClient = 0m,
            TotalFinancialProducts = 0,
            ActiveLoans = 0,
            ActiveCreditCards = 0,
            ActiveSavingsAccounts = 0
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
            IsActive = false 
        };
  
        _dummyUsers.Insert(0, newUser);

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

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Identification = model.Identification;
        user.Email = model.Email;
        user.Username = model.Username;
        
        return RedirectToAction(nameof(UserManagement));
    }

    // LOAN MODULE

    private static List<LoanViewModel> _dummyLoans = new List<LoanViewModel>
    {
        new LoanViewModel { Id = "1", LoanNumber = "948271053", ClientName = "Roberto Carlos", ClientCedula = "0912345678", ApprovedCapital = 15000.00m, TotalInstallments = 48, PaidInstallments = 12, PendingAmount = 11250.00m, InterestRate = 14.5m, TermInMonths = 48, LoanStatus = "Activo", ClientStatus = "Al día" },
        new LoanViewModel { Id = "2", LoanNumber = "837492011", ClientName = "María Suárez", ClientCedula = "1723456789", ApprovedCapital = 5500.00m, TotalInstallments = 24, PaidInstallments = 8, PendingAmount = 3666.67m, InterestRate = 12.0m, TermInMonths = 24, LoanStatus = "Activo", ClientStatus = "En mora" },
        new LoanViewModel { Id = "3", LoanNumber = "726354890", ClientName = "Juan Gómez", ClientCedula = "0102345678", ApprovedCapital = 2000.00m, TotalInstallments = 12, PaidInstallments = 12, PendingAmount = 0.00m, InterestRate = 16.0m, TermInMonths = 12, LoanStatus = "Completado", ClientStatus = "Al día" }
    };

   [HttpGet]
    public IActionResult LoanManagement(string statusFilter = "Activos", string searchCedula = "", int page = 1)
    {
        var query = _dummyLoans.AsQueryable();

        if (statusFilter == "Activos")
        {
            query = query.Where(l => l.LoanStatus == "Activo");
        }
        else if (statusFilter == "Completados")
        {
            query = query.Where(l => l.LoanStatus == "Completado");
        }

        if (!string.IsNullOrEmpty(searchCedula))
        {
            query = query.Where(l => l.ClientCedula.Contains(searchCedula));
            
            if (!query.Any())
            {
                ViewBag.SearchMessage = "No existe un cliente registrado con esta cédula o este cliente no tiene préstamos registrados.";
            }
        }

        query = query.OrderBy(l => l.LoanStatus == "Completado" ? 1 : 0).ThenByDescending(l => l.Id);

        var loans = query.ToList();
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
    public IActionResult AssignLoanStep1()
    {
        var model = new AssignLoanStep1ViewModel
        {
            AverageSystemDebt = 125450.00m, 
            EligibleClients = new List<ClientSelectionViewModel>
            {
                new ClientSelectionViewModel { Id = "c1", Cedula = "402-1234567-8", FullName = "Juan Pérez Domínguez", Email = "juan.perez@email.com", TotalDebt = 0.00m },
                new ClientSelectionViewModel { Id = "c2", Cedula = "001-9876543-2", FullName = "María Rodríguez Alba", Email = "m.rodriguez@empresa.com.do", TotalDebt = 15200.50m },
                new ClientSelectionViewModel { Id = "c3", Cedula = "031-4567890-1", FullName = "Carlos Sánchez Mella", Email = "csanchez88@gmail.com", TotalDebt = 5000.00m }
            }
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
        decimal r = (model.InterestRate / 100) / 12;
        int n = model.TermInMonths;
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
        TempData["SuccessMessage"] = "Préstamo de alto riesgo asignado bajo su autorización.";
        return RedirectToAction(nameof(LoanManagement));
    }

    [HttpGet]
    public IActionResult LoanDetails(string id)
    {
        var model = new LoanDetailsViewModel
        {
            LoanNumber = "PR-2026-8942",
            ClientName = "TechCorp Solutions...",
            ApprovedAmount = 150000.00m,
            InterestRate = 8.5m,
            TermInMonths = 36,
            LoanStatus = "Activo",
            PendingBalance = 82500.00m,
            MonthlyQuote = 4735.25m,
            StartDate = new DateTime(2026, 1, 12),
            NextDueDate = new DateTime(2026, 11, 12),
            PaymentProgress = 45,
            AmortizationTable = new List<AmortizationRowViewModel>
            {
                new AmortizationRowViewModel { InstallmentNumber = 1, DueDate = new DateTime(2026, 2, 12), InstallmentValue = 4735.25m, InterestAmount = 1062.50m, CapitalAmount = 3672.75m, PendingBalance = 146327.25m, PaymentStatus = "Pagada", IsOverdue = false },
                new AmortizationRowViewModel { InstallmentNumber = 2, DueDate = new DateTime(2026, 3, 12), InstallmentValue = 4735.25m, InterestAmount = 1036.48m, CapitalAmount = 3698.77m, PendingBalance = 142628.48m, PaymentStatus = "Pagada", IsOverdue = false },
                new AmortizationRowViewModel { InstallmentNumber = 9, DueDate = new DateTime(2026, 10, 12), InstallmentValue = 4735.25m, InterestAmount = 835.40m, CapitalAmount = 3899.85m, PendingBalance = 114205.10m, PaymentStatus = "Parcial", IsOverdue = false },
                new AmortizationRowViewModel { InstallmentNumber = 10, DueDate = new DateTime(2026, 11, 12), InstallmentValue = 4735.25m, InterestAmount = 807.53m, CapitalAmount = 3927.72m, PendingBalance = 110277.38m, PaymentStatus = "Pendiente", IsOverdue = true },
                new AmortizationRowViewModel { InstallmentNumber = 11, DueDate = new DateTime(2026, 12, 12), InstallmentValue = 4735.25m, InterestAmount = 779.60m, CapitalAmount = 3955.65m, PendingBalance = 106321.73m, PaymentStatus = "Pendiente", IsOverdue = false }
            }
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult EditLoanRate(string id)
    {
        var loan = _dummyLoans.FirstOrDefault(l => l.Id == id);
        if (loan == null)
        {
            TempData["ErrorMessage"] = "El préstamo seleccionado no existe.";
            return RedirectToAction(nameof(LoanManagement));
        }

        if (loan.LoanStatus != "Activo")
        {
            TempData["ErrorMessage"] = "Solo se puede modificar la tasa de interés de préstamos activos.";
            return RedirectToAction(nameof(LoanManagement));
        }

        var model = new EditLoanRateViewModel
        {
            Id = loan.Id,
            InterestRate = loan.InterestRate
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditLoanRate(EditLoanRateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

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
        var model = new CreditCardDetailsViewModel
        {
            MaskedNumber = "**** **** **** 8924", ClientName = "ALEXANDER WRIGHT", ExpirationDate = "11/27",
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
        if (model.NewLimit < model.CurrentDebt)
        {
            ModelState.AddModelError("NewLimit", "El límite de la tarjeta no puede ser inferior al monto adeudado actualmente.");
            return View(model);
        }
        if (!ModelState.IsValid) return View(model);

        TempData["SuccessMessage"] = "Límite actualizado correctamente.";
        return RedirectToAction(nameof(CreditCardManagement));
    }

    [HttpGet]
    public IActionResult CancelCreditCard(string id)
    {
        var card = _dummyCards.FirstOrDefault(c => c.Id == id);
        if (card == null) return RedirectToAction(nameof(CreditCardManagement));
        
        return View(new CancelCreditCardViewModel { Id = card.Id, MaskedNumber = card.MaskedNumber.Substring(card.MaskedNumber.Length - 4), CurrentDebt = card.DebtAmount });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CancelCreditCardConfirmed(string id)
    {
        var card = _dummyCards.FirstOrDefault(c => c.Id == id);
        if (card != null && card.DebtAmount > 0)
        {
            TempData["ErrorMessage"] = "Para cancelar esta tarjeta, el cliente debe saldar la totalidad de la deuda pendiente.";
            return RedirectToAction(nameof(CancelCreditCard), new { id });
        }

        TempData["SuccessMessage"] = "Tarjeta cancelada exitosamente.";
        return RedirectToAction(nameof(CreditCardManagement));
    }
}