namespace Application.Services;

public class HighRiskConflictException : Exception
{
    public string RiskType { get; }
    public decimal CurrentDebt { get; }
    public decimal ProjectedDebt { get; }
    public decimal AverageDebt { get; }

    public HighRiskConflictException(string riskType, decimal currentDebt, decimal projectedDebt, decimal averageDebt, string message)
        : base(message)
    {
        RiskType = riskType;
        CurrentDebt = currentDebt;
        ProjectedDebt = projectedDebt;
        AverageDebt = averageDebt;
    }
}
