namespace Application.Models.ViewModels.Client;

public class ClientHomeViewModel
{
    public List<ClientAccountViewModel> Accounts { get; set; } = new();
    public List<ClientLoanViewModel> Loans { get; set; } = new();
    public List<ClientCardViewModel> Cards { get; set; } = new();
}

public class ClientAccountViewModel
{
    public string Id { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public bool IsPrincipal { get; set; }
}

public class ClientLoanViewModel
{
    public string Id { get; set; } = string.Empty;
    public string LoanNumber { get; set; } = string.Empty;
    public decimal ApprovedAmount { get; set; }
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal InterestRate { get; set; }
    public int TermInMonths { get; set; }
    public bool IsInMora { get; set; } 
}

public class ClientCardViewModel
{
    public string Id { get; set; } = string.Empty;
    public string MaskedNumber { get; set; } = string.Empty; 
    public decimal CreditLimit { get; set; }
    public string ExpirationDate { get; set; } = string.Empty;
    public decimal DebtAmount { get; set; }
}


public class ClientAccountDetailsViewModel
{
    public string AccountNumber { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public bool IsPrincipal { get; set; }
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

public class ClientLoanDetailsViewModel
{
    public string LoanNumber { get; set; } = string.Empty;
    public decimal PendingAmount { get; set; }
    public List<AmortizationViewModel> AmortizationTable { get; set; } = new();
}

public class AmortizationViewModel
{
    public DateTime DueDate { get; set; }
    public decimal InstallmentValue { get; set; }
    public string Status { get; set; } = string.Empty; 
    public bool IsOverdue { get; set; }
}

public class ClientCardDetailsViewModel
{
    public string MaskedNumber { get; set; } = string.Empty;
    public decimal DebtAmount { get; set; }
    public List<ConsumptionViewModel> Consumptions { get; set; } = new();
}

public class ConsumptionViewModel
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Commerce { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}