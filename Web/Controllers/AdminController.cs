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
}