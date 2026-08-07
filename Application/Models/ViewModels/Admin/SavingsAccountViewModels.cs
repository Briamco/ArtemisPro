using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Admin;


public class SavingsAccountListViewModel
{
    public List<SavingsAccountViewModel> Accounts { get; set; } = new();
    public string CurrentStatusFilter { get; set; } = "Activas";
    public string CurrentTypeFilter { get; set; } = "Todas";
    public string SearchCedula { get; set; } = string.Empty;
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalRecords { get; set; } = 0;
}

public class SavingsAccountViewModel
{
    public string Id { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientCedula { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string AccountType { get; set; } = string.Empty; 
    public string Status { get; set; } = string.Empty; 
}

// step 1
public class AssignSavingsAccountStep1ViewModel
{
    public string SearchCedula { get; set; } = string.Empty;
    public List<ClientSelectionViewModel> EligibleClients { get; set; } = new();

    [Required(ErrorMessage = "Debe seleccionar un cliente para continuar.")]
    public string SelectedClientId { get; set; } = string.Empty;
}

// step 2
public class AssignSavingsAccountStep2ViewModel
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientCedula { get; set; } = string.Empty;

    [Required(ErrorMessage = "El balance inicial es requerido.")]
    [Range(0.0, double.MaxValue, ErrorMessage = "El balance inicial no puede ser negativo.")]
    public decimal InitialBalance { get; set; }
}

// account details 
public class SavingsAccountDetailsViewModel
{
    public string AccountNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public List<TransactionViewModel> Transactions { get; set; } = new();
}

public class TransactionViewModel
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty; 
    public string Beneficiary { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; 
}

// cancelation of savings account
public class CancelSavingsAccountViewModel
{
    public string Id { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
}