using System.Threading.Tasks;

namespace Shared.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}
