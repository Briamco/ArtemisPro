namespace Application.DTOs.Identity;

public class ForgotPasswordResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
}
