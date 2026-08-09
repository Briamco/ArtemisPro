using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class CreditCardAppService : ICreditCardAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreditCardAppService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CreditCardDto>> GetCreditCardsAsync(string? status = null, string? cedula = null)
    {
        var cards = await _unitOfWork.CreditCards.GetAllAsync();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CardStatus>(status, true, out var cardStatus))
        {
            cards = cards.Where(c => c.Status == cardStatus);
        }

        if (!string.IsNullOrEmpty(cedula))
        {
            var user = await _unitOfWork.Users.GetByCedulaAsync(cedula);
            if (user != null)
            {
                cards = cards.Where(c => c.ClientId == user.Id);
            }
            else
            {
                return Enumerable.Empty<CreditCardDto>();
            }
        }

        return _mapper.Map<IEnumerable<CreditCardDto>>(cards);
    }

    public async Task<CreditCardDto?> GetCreditCardByIdAsync(Guid id)
    {
        var card = await _unitOfWork.CreditCards.GetByIdAsync(id);
        return card == null ? null : _mapper.Map<CreditCardDto>(card);
    }

    public async Task<IEnumerable<CreditCardTransactionDto>> GetTransactionsAsync(Guid cardId)
    {
        var transactions = await _unitOfWork.CreditCardTransactions.FindAsync(t => t.CreditCardId == cardId);
        return _mapper.Map<IEnumerable<CreditCardTransactionDto>>(transactions);
    }

    public async Task<(bool Success, string? Error)> AssignCreditCardAsync(AssignCreditCardDto dto)
    {
        var client = await _unitOfWork.Users.GetByIdAsync(dto.ClientId);
        if (client == null)
            return (false, "El cliente no existe.");

        var cardNumber = await GenerateUniqueCardNumberAsync();
        var expirationDate = DateTime.UtcNow.AddYears(3).ToString("MM/yy");
        var cvc = GenerateCvc();
        var cvcHash = HashCvc(cvc);

        var card = new CreditCard
        {
            ClientId = dto.ClientId,
            CardNumber = cardNumber,
            Limit = dto.Limit,
            Debt = 0,
            ExpirationDate = expirationDate,
            CvcHash = cvcHash,
            Status = CardStatus.Activa,
            CreatedAt = DateTime.UtcNow,
            AdminId = Guid.Empty // Set dynamically if the context is available
        };

        await _unitOfWork.CreditCards.AddAsync(card);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateCreditCardLimitAsync(Guid id, UpdateCreditCardLimitDto dto)
    {
        var card = await _unitOfWork.CreditCards.GetByIdAsync(id);
        if (card == null)
            return (false, "Tarjeta de crédito no encontrada.");

        card.Limit = dto.NewLimit;
        _unitOfWork.CreditCards.Update(card);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CancelCreditCardAsync(Guid id)
    {
        var card = await _unitOfWork.CreditCards.GetByIdAsync(id);
        if (card == null)
            return (false, "Tarjeta de crédito no encontrada.");

        if (card.Debt > 0)
            return (false, "No se puede cancelar una tarjeta con deuda activa.");

        card.Status = CardStatus.Cancelada;
        _unitOfWork.CreditCards.Update(card);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    private async Task<string> GenerateUniqueCardNumberAsync()
    {
        var random = new Random();
        string cardNumber;
        bool exists;

        do
        {
            // Genera 16 dígitos. Empieza con 4 (simulando Visa) u otro número, por simplicidad usamos una secuencia de 16 dígitos aleatoria
            var firstPart = random.Next(10000000, 99999999).ToString("D8");
            var secondPart = random.Next(10000000, 99999999).ToString("D8");
            cardNumber = firstPart + secondPart;

            exists = await _unitOfWork.CreditCards.ExistsAsync(c => c.CardNumber == cardNumber);
        } while (exists);

        return cardNumber;
    }

    private string GenerateCvc()
    {
        return new Random().Next(100, 999).ToString("D3");
    }

    private string HashCvc(string cvc)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(cvc);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }
}
