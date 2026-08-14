namespace Application.DTOs.Banking;

public class CashierSystemAccountDto
{
    public string AccountNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

public class CashierSystemCardDto
{
    public string CardNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Debt { get; set; }
}

public class CashierSystemLoanDto
{
    public string LoanNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; 
    public decimal PendingAmount { get; set; }
    public bool HasPendingInstallments { get; set; }
}
