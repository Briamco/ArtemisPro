using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Client;

// Express
public class TransactionExpressViewModel
{
    [Required(ErrorMessage = "La cuenta de origen es requerida.")]
    public string SourceAccountId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
    public string DestinationAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a transferir es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
    public decimal Amount { get; set; }

    public List<ClientAccountViewModel> MyActiveAccounts { get; set; } = new();
}

public class ConfirmTransactionExpressViewModel
{
    public string SourceAccountId { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public string DestinationOwnerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

// card payment 
public class PayCreditCardViewModel
{
    [Required(ErrorMessage = "La cuenta de origen es requerida.")]
    public string SourceAccountId { get; set; } = string.Empty;

    [Required(ErrorMessage = "La tarjeta de crédito destino es requerida.")]
    public string CreditCardId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a pagar es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
    public decimal Amount { get; set; }

    public List<ClientAccountViewModel> MyActiveAccounts { get; set; } = new();
    public List<ClientCardViewModel> MyActiveCards { get; set; } = new();
}

// loan payment
public class PayLoanViewModel
{
    [Required(ErrorMessage = "La cuenta de origen es requerida.")]
    public string SourceAccountId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El préstamo a pagar es requerido.")]
    public string LoanId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a pagar es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
    public decimal Amount { get; set; }

    public List<ClientAccountViewModel> MyActiveAccounts { get; set; } = new();
    public List<ClientLoanViewModel> MyActiveLoans { get; set; } = new();
}

// benefiaciary
public class TransactionBeneficiaryViewModel
{
    [Required(ErrorMessage = "La cuenta de origen es requerida.")]
    public string SourceAccountId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El beneficiario es requerido.")]
    public string BeneficiaryId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto a transferir es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
    public decimal Amount { get; set; }

    public List<ClientAccountViewModel> MyActiveAccounts { get; set; } = new();
    public List<BeneficiaryViewModel> MyBeneficiaries { get; set; } = new();
}

public class ConfirmTransactionBeneficiaryViewModel
{
    public string SourceAccountId { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public string DestinationOwnerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}