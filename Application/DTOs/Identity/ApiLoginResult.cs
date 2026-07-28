using System;

namespace Application.DTOs.Identity;

public class ApiLoginResult
{
    public bool Succeeded { get; set; }
    public string? Token { get; set; }
    public DateTime Expires { get; set; }
    public string? ErrorMessage { get; set; }
}
