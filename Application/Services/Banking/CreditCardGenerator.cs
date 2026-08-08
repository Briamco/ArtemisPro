using System;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services.Banking;

public static class CreditCardGenerator
{
    /// <summary>
    /// Genera un identificador único de 16 dígitos.
    /// </summary>
    public static string GenerateCardNumber()
    {
        var random = new Random();
        var builder = new StringBuilder();
        
        for (int i = 0; i < 16; i++)
        {
            builder.Append(random.Next(0, 10));
        }
        
        return builder.ToString();
    }

    /// <summary>
    /// Calcula la fecha de expiración automática agregando 3 años a la fecha actual.
    /// Formato: MM/yy
    /// </summary>
    public static string CalculateExpirationDate()
    {
        var expiration = DateTime.UtcNow.AddYears(3);
        return expiration.ToString("MM/yy");
    }

    /// <summary>
    /// Genera un código CVC aleatorio y su respectivo hash para almacenamiento seguro.
    /// </summary>
    public static (string Cvc, string CvcHash) GenerateCvc()
    {
        var random = new Random();
        var cvc = random.Next(100, 1000).ToString("D3");
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(cvc));
        var cvcHash = Convert.ToBase64String(hashBytes);
        
        return (cvc, cvcHash);
    }
}
