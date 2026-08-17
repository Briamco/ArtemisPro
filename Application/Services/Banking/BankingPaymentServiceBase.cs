using System;
using System.Threading.Tasks;
using Domain.Entities;
using Shared.Interfaces;

namespace Application.Services.Banking;

public abstract class BankingPaymentServiceBase
{
    protected readonly IEmailService _emailService;

    protected BankingPaymentServiceBase(IEmailService emailService)
    {
        _emailService = emailService;
    }

    protected async Task<bool> SendAsync(string? to, string subject, string body)
    {
        if (string.IsNullOrEmpty(to))
        {
            return true;
        }

        try
        {
            await _emailService.SendAsync(to, subject, body);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    protected string GetLast4(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length >= 4 ? value.Substring(value.Length - 4) : value;
    }

    protected string BuildOwnerName(ApplicationUser? client)
    {
        if (client == null) return string.Empty;
        return $"{client.FirstName} {client.LastName}".Trim();
    }
}
