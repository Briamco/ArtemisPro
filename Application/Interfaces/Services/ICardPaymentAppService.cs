using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

public interface ICardPaymentAppService
{
    Task<CardPaymentPreviewResult> GetCardPaymentPreviewAsync(string accountNumber, string cardNumber, decimal amount);
    Task<CardPaymentResult> CreateCardPaymentAsync(System.Guid tellerId, CreateCardPaymentDto dto);
}
