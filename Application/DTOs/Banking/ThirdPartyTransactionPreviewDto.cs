namespace Application.DTOs.Banking;

public class ThirdPartyTransactionPreviewDto
{
    public string SourceAccountOwner { get; set; } = string.Empty;
    public string SourceAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountOwner { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class ThirdPartyTransactionPreviewResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public ThirdPartyTransactionPreviewDto? Preview { get; set; }
}
