namespace Application.DTOs.Banking;

public class LoanPaymentPreviewDto
{
    public string OriginAccountNumber { get; set; } = string.Empty;
    public string OriginAccountClientName { get; set; } = string.Empty;
    public string LoanNumber { get; set; } = string.Empty;
    public string LoanClientName { get; set; } = string.Empty;
    public decimal EnteredAmount { get; set; }
    public decimal EffectiveAmount { get; set; }
}

public class LoanPaymentPreviewResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public LoanPaymentPreviewDto? Preview { get; set; }
}
