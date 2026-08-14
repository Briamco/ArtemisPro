namespace Application.DTOs.Banking;

public class TransferResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public bool EmailSent { get; set; } = true;
}
