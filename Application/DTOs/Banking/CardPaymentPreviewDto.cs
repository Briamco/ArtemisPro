namespace Application.DTOs.Banking;

public class CardPaymentPreviewDto
{
    public string OriginAccountNumber { get; set; } = string.Empty;
    public string OriginAccountClientName { get; set; } = string.Empty;
    public string CardLast4 { get; set; } = string.Empty;
    public string CardClientName { get; set; } = string.Empty;
    public decimal EnteredAmount { get; set; }
    public decimal EffectiveAmount { get; set; }
}

public class CardPaymentPreviewResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public CardPaymentPreviewDto? Preview { get; set; }
}
