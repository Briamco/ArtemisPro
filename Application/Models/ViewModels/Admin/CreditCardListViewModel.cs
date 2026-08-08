namespace Application.Models.ViewModels.Admin;

public class CreditCardListViewModel
{
    public List<CreditCardViewModel> Cards { get; set; } = new();
    public string CurrentFilter { get; set; } = "Activas";
    public string SearchCedula { get; set; } = string.Empty;
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalRecords { get; set; } = 0;
    
    public int TotalActiveCards { get; set; }
    public decimal TotalAccumulatedDebt { get; set; }
    public string PortfolioRisk { get; set; } = "Bajo";
}

public class CreditCardViewModel
{
    public string Id { get; set; } = string.Empty;
    public string MaskedNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientCedula { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public string ExpirationDate { get; set; } = string.Empty;
    public decimal DebtAmount { get; set; }
    public string Status { get; set; } = string.Empty; 
}