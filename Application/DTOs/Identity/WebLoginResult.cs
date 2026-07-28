using System;

namespace Application.DTOs.Identity;

public class WebLoginResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RedirectController { get; set; }
    public string? RedirectAction { get; set; }
}
