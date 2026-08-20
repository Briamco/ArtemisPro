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
using Web.Helpers;

namespace Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    private readonly Application.Interfaces.Services.ICreditCardAppService _creditCardService;
    private readonly Application.Interfaces.Services.IUserAppService _userService;
    private readonly ILoanAppService _loanAppService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISavingsAccountAppService _savingsAccountAppService;
    private readonly IAdminDashboardAppService _adminDashboardAppService;

    public AdminController(
        Application.Interfaces.Services.ICreditCardAppService creditCardService, 
        Application.Interfaces.Services.IUserAppService userService,
        ILoanAppService loanAppService, 
        UserManager<ApplicationUser> userManager,
        ISavingsAccountAppService savingsAccountAppService,
        IAdminDashboardAppService adminDashboardAppService)
    {
        _creditCardService = creditCardService;
        _userService = userService;
        _loanAppService = loanAppService;
        _userManager = userManager;
        _savingsAccountAppService = savingsAccountAppService;
        _adminDashboardAppService = adminDashboardAppService;
    }

    public async Task<IActionResult> Index()
    {
        var stats = await _adminDashboardAppService.GetGeneralStatsAsync();

        var dashboardData = new AdminDashboardViewModel
        {
            TotalHistoricalTransactions = stats.TotalTransaccionesHistoricas,
            DailyTransactions = stats.TransaccionesDelDia,
            TotalHistoricalPayments = stats.TotalPagosHistoricos,
            DailyPayments = stats.PagosDelDia,
            ActiveClients = stats.ClientesActivos,
            InactiveClients = stats.ClientesInactivos,
            AverageDebtPerClient = stats.MontoPromedioDeuda,
            TotalFinancialProducts = stats.TotalProductosFinancieros,
            ActiveLoans = stats.PrestamosVigentes,
            ActiveCreditCards = stats.TarjetasCreditoActivas,
            ActiveSavingsAccounts = stats.CuentasAhorroActivas
        };

        return View(dashboardData);
    }

    [HttpGet]
    public async Task<IActionResult> UserManagement(string roleFilter = "Todos", int page = 1)
    {
        var filterRole = roleFilter == "Todos" ? null : roleFilter;
        var usersDto = await _userService.GetAllUsersAsync(filterRole);

        const int pageSize = 20;
        var totalRecords = usersDto.Count();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var pagedUsers = usersDto
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserViewModel
            {
                Id = u.Id.ToString(),
                Username = u.UserName,
                Identification = u.Cedula,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive
            })
            .ToList();

        var model = new UserListViewModel
        {
            Users = pagedUsers,
            CurrentFilter = roleFilter,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalRecords = totalRecords
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
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var confirmationLinkFormat = Url.Action("Activate", "Account", new { email = "{0}", token = "{1}" }, Request.Scheme) 
                                     ?? $"{Request.Scheme}://{Request.Host}/Account/Activate?email={{0}}&token={{1}}";

        var dto = new Application.DTOs.Identity.CreateUserDto
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Cedula = model.Identification,
            Email = model.Email,
            UserName = model.Username,
            Password = model.Password,
            Role = model.Role,
            InitialBalance = model.InitialAmount ?? 0m
        };

        var (success, error) = await _userService.CreateUserAsync(dto, confirmationLinkFormat);
        if (!success)
        {
            TempData["ErrorMessage"] = error;
            return View(model);
        }

        TempData["SuccessMessage"] = "Usuario creado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    //edit user
    [HttpGet]
    public async Task<IActionResult> EditUser(string id)
    {
        if (!Guid.TryParse(id, out var userGuid))
        {
            TempData["ErrorMessage"] = "El usuario seleccionado no existe.";
            return RedirectToAction(nameof(UserManagement));
        }

        var user = await _userService.GetUserByIdAsync(userGuid);
        if (user == null)
        {
            TempData["ErrorMessage"] = "El usuario seleccionado no existe.";
            return RedirectToAction(nameof(UserManagement));
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser != null && (currentUser.Id == userGuid || currentUser.UserName == user.UserName))
        {
            TempData["ErrorMessage"] = "No puede editar su propia cuenta desde este módulo.";
            return RedirectToAction(nameof(UserManagement));
        }

        var model = new EditUserViewModel
        {
            Id = user.Id.ToString(),
            FirstName = user.FirstName,
            LastName = user.LastName,
            Identification = user.Cedula,
            Email = user.Email,
            Username = user.UserName,
            Role = user.Role
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (!Guid.TryParse(model.Id, out var userGuid))
        {
            TempData["ErrorMessage"] = "El usuario seleccionado no existe.";
            return RedirectToAction(nameof(UserManagement));
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser != null && (currentUser.Id == userGuid || currentUser.UserName == model.Username))
        {
            TempData["ErrorMessage"] = "No puede editar su propia cuenta desde este módulo.";
            return RedirectToAction(nameof(UserManagement));
        }

        var dto = new Application.DTOs.Identity.EditUserDto
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Cedula = model.Identification,
            Email = model.Email,
            UserName = model.Username,
            NewPassword = model.NewPassword,
            ConfirmPassword = model.ConfirmNewPassword,
            AdditionalAmount = model.AdditionalAmount ?? 0m
        };

        var (success, error) = await _userService.EditUserAsync(userGuid, dto);
        if (!success)
        {
            TempData["ErrorMessage"] = error;
            return View(model);
        }

        TempData["SuccessMessage"] = "Usuario actualizado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        if (!Guid.TryParse(id, out var userGuid))
        {
            TempData["ErrorMessage"] = "El usuario seleccionado no existe.";
            return RedirectToAction(nameof(UserManagement));
        }

        var user = await _userService.GetUserByIdAsync(userGuid);
        if (user == null)
        {
            TempData["ErrorMessage"] = "El usuario seleccionado no existe.";
            return RedirectToAction(nameof(UserManagement));
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser != null && (currentUser.Id == userGuid || currentUser.UserName == user.UserName))
        {
            TempData["ErrorMessage"] = "No puede modificar el estado de su propia cuenta.";
            return RedirectToAction(nameof(UserManagement));
        }

        var (success, error) = await _userService.ToggleUserStatusAsync(userGuid);
        if (!success)
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(UserManagement));
        }

        TempData["SuccessMessage"] = !user.IsActive 
            ? "Usuario activado exitosamente." 
            : "Usuario inactivado exitosamente.";

        return RedirectToAction(nameof(UserManagement));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateUser(string id)
    {
        return await ToggleUserStatus(id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InactivateUser(string id)
    {
        return await ToggleUserStatus(id);
    }

    [HttpGet]
    public async Task<IActionResult> LoanManagement(string statusFilter = "Activos", string searchCedula = "", int page = 1)
    {
        var apiStatus = statusFilter == "Activos" ? "activos" : statusFilter == "Completados" ? "completados" : "todos";
        var result = await _loanAppService.GetLoansAsync(page, 20, apiStatus, string.IsNullOrEmpty(searchCedula) ? null : searchCedula);

        var clientIds = result.Data.Select(l => l.ClientId).Distinct().ToList();
        var clients = new Dictionary<Guid, ApplicationUser>();
        foreach (var cid in clientIds)
        {
            var user = await _userManager.FindByIdAsync(cid.ToString());
            if (user != null) clients[cid] = user;
        }

        var loanList = new List<LoanViewModel>();
        foreach (var l in result.Data)
        {
            clients.TryGetValue(l.ClientId, out var user);
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
        var allLoans = (await _loanAppService.GetAllLoansAsync()).ToList();

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
        return RedirectToAction(nameof(Index));
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
        return RedirectToAction(nameof(Index));
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
        return RedirectToAction(nameof(Index));
    }

    // CREDIT CARD MANAGMENT

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
                ClientName = c.ClientFullName,
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
            return RedirectToAction(nameof(Index));
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
            ClientName = card.ClientFullName,
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
            return RedirectToAction(nameof(Index));
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
        return RedirectToAction(nameof(Index));
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
        return RedirectToAction(nameof(Index));
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
                Beneficiary = TransactionHelpers.ResolveTransactionBeneficiary(t.Beneficiary),
                Origin = TransactionHelpers.ResolveTransactionOrigin(t.Origin),
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

        return RedirectToAction(nameof(Index));
    }
}