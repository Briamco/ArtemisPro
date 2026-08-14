namespace Application.DTOs.Banking;

public class DepositResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public bool EmailSent { get; set; } = true;
}
