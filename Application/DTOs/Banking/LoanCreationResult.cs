namespace Application.DTOs.Banking;

public class LoanCreationResult 
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsHighRiskConflict { get; set; }
    public string? RiskType { get; set; }
    public decimal CurrentDebt { get; set; }
    public decimal ProjectedDebt { get; set; }
    public decimal AverageDebt { get; set; }
}
