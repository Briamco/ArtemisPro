using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services.Banking;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Shared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Application.Services;

public class CreditCardAppService : ICreditCardAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreditCardAppService(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _emailService = emailService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResultDto<CreditCardDto>> GetCreditCardsPagedAsync(int page, int pageSize, string? status, string? identification)
    {
        var allCards = (await _unitOfWork.CreditCards.GetAllAsync()).ToList();
        var users = (await _unitOfWork.Users.GetAllAsync()).ToList();
        var userDict = users.ToDictionary(u => u.Id);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CardStatus>(status, true, out var parsedStatus))
            allCards = allCards.Where(c => c.Status == parsedStatus).ToList();

        if (!string.IsNullOrEmpty(identification))
        {
            var user = await _unitOfWork.Users.GetByCedulaAsync(identification);
            if (user != null)
                allCards = allCards.Where(c => c.ClientId == user.Id).ToList();
            else
                return new PagedResultDto<CreditCardDto>();
        }

        var ordered = allCards.OrderByDescending(c => c.CreatedAt).ToList();
        var totalRecords = ordered.Count;
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        if (page < 1) page = 1;
        var paged = ordered.Skip((page - 1) * pageSize).Take(pageSize);

        return new PagedResultDto<CreditCardDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            Data = paged.Select(c => new CreditCardDto
            {
                Id = c.Id,
                MaskedCardNumber = MaskCardNumber(c.CardNumber),
                LastFourDigits = c.CardNumber.Length >= 4 ? c.CardNumber[^4..] : c.CardNumber,
                ClientId = c.ClientId,
                ClientFullName = userDict.ContainsKey(c.ClientId)
                    ? $"{userDict[c.ClientId].FirstName} {userDict[c.ClientId].LastName}"
                    : "",
                CreditLimit = c.Limit,
                AvailableCredit = c.Limit - c.Debt,
                CurrentDebt = c.Debt,
                ExpirationDate = c.ExpirationDate,
                Status = c.Status.ToString(),
                CreatedAt = c.CreatedAt
            }).ToList()
        };
    }

    public async Task<CreditCardDetailDto?> GetCreditCardDetailByIdAsync(Guid id)
    {
        var c = await _unitOfWork.CreditCards.GetByIdAsync(id);
        if (c == null) return null;

        var user = await _userManager.FindByIdAsync(c.ClientId.ToString());
        var txs = await _unitOfWork.CreditCardTransactions.GetByCreditCardIdAsync(id);
        var sortedTxs = txs.OrderByDescending(t => t.Date).ToList();

        return new CreditCardDetailDto
        {
            Id = c.Id,
            MaskedCardNumber = MaskCardNumber(c.CardNumber),
            LastFourDigits = c.CardNumber.Length >= 4 ? c.CardNumber[^4..] : c.CardNumber,
            ClientId = c.ClientId,
            ClientFullName = user != null ? $"{user.FirstName} {user.LastName}" : "",
            CreditLimit = c.Limit,
            AvailableCredit = c.Limit - c.Debt,
            CurrentDebt = c.Debt,
            ExpirationDate = c.ExpirationDate,
            Status = c.Status.ToString(),
            CreatedAt = c.CreatedAt,
            Consumptions = sortedTxs.Select(t => new CreditCardTransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                CommerceName = t.MerchantName,
                Status = t.Status.ToString(),
                Date = t.Date
            }).ToList()
        };
    }

    public async Task<IEnumerable<CreditCardDto>> GetCreditCardsAsync(string? status = null, string? cedula = null)
    {
        var cards = await _unitOfWork.CreditCards.GetAllAsync();

        var users = await _unitOfWork.Users.GetAllAsync();
        var userDict = users.ToDictionary(u => u.Id);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CardStatus>(status, true, out var parsedStatus))
            cards = cards.Where(c => c.Status == parsedStatus).ToList();

        if (!string.IsNullOrEmpty(cedula))
        {
            var user = await _unitOfWork.Users.GetByCedulaAsync(cedula);
            if (user != null)
                cards = cards.Where(c => c.ClientId == user.Id).ToList();
            else
                return new List<CreditCardDto>();
        }

        return cards.Select(c => new CreditCardDto
        {
            Id = c.Id,
            MaskedCardNumber = MaskCardNumber(c.CardNumber),
            LastFourDigits = c.CardNumber.Length >= 4 ? c.CardNumber[^4..] : c.CardNumber,
            ClientId = c.ClientId,
            ClientFullName = userDict.ContainsKey(c.ClientId)
                ? $"{userDict[c.ClientId].FirstName} {userDict[c.ClientId].LastName}"
                : "",
            CreditLimit = c.Limit,
            AvailableCredit = c.Limit - c.Debt,
            CurrentDebt = c.Debt,
            ExpirationDate = c.ExpirationDate,
            Status = c.Status.ToString(),
            CreatedAt = c.CreatedAt
        }).OrderByDescending(c => c.CreatedAt);
    }

    public async Task<CreditCardDto?> GetCreditCardByIdAsync(Guid id)
    {
        var c = await _unitOfWork.CreditCards.GetByIdAsync(id);
        if (c == null) return null;

        var user = await _userManager.FindByIdAsync(c.ClientId.ToString());
        return new CreditCardDto
        {
            Id = c.Id,
            MaskedCardNumber = MaskCardNumber(c.CardNumber),
            LastFourDigits = c.CardNumber.Length >= 4 ? c.CardNumber[^4..] : c.CardNumber,
            ClientId = c.ClientId,
            ClientFullName = user != null ? $"{user.FirstName} {user.LastName}" : "",
            CreditLimit = c.Limit,
            AvailableCredit = c.Limit - c.Debt,
            CurrentDebt = c.Debt,
            ExpirationDate = c.ExpirationDate,
            Status = c.Status.ToString(),
            CreatedAt = c.CreatedAt
        };
    }

    public async Task<IEnumerable<CreditCardTransactionDto>> GetTransactionsAsync(Guid cardId)
    {
        var txs = await _unitOfWork.CreditCardTransactions.GetByCreditCardIdAsync(cardId);
        return txs.Select(t => new CreditCardTransactionDto
        {
            Id = t.Id,
            Amount = t.Amount,
            CommerceName = t.MerchantName,
            Status = t.Status.ToString(),
            Date = t.Date
        }).OrderByDescending(t => t.Date);
    }

    public async Task<(bool Success, string? Error, CreditCardDto? Card)> AssignCreditCardAsync(AssignCreditCardDto dto)
    {
        var client = await _userManager.FindByIdAsync(dto.ClientId.ToString());
        if (client == null) return (false, "Cliente no encontrado.", null);
        if (!client.IsActive) return (false, "El cliente no está activo.", null);
        if (dto.Limit <= 0) return (false, "El límite debe ser mayor a cero.", null);

        string cardNumber;
        bool exists;
        do
        {
            cardNumber = CreditCardGenerator.GenerateCardNumber();
            exists = (await _unitOfWork.CreditCards.GetByCardNumberAsync(cardNumber)) != null;
        } while (exists);

        var cvcData = CreditCardGenerator.GenerateCvc();
        var expDate = CreditCardGenerator.CalculateExpirationDate();

        Guid adminId = Guid.Empty;
        var currentUser = _httpContextAccessor.HttpContext?.User;
        if (currentUser?.Identity?.IsAuthenticated == true)
        {
            var adminUser = await _userManager.GetUserAsync(currentUser);
            if (adminUser != null) adminId = adminUser.Id;
        }

        if (adminId == Guid.Empty)
        {
            var admins = await _userManager.GetUsersInRoleAsync("Administrador");
            var admin = admins.FirstOrDefault();
            if (admin != null) adminId = admin.Id;
        }

        var card = new CreditCard
        {
            ClientId = client.Id,
            CardNumber = cardNumber,
            Limit = dto.Limit,
            Debt = 0.00m,
            ExpirationDate = expDate,
            CvcHash = cvcData.CvcHash,
            Status = CardStatus.Activa,
            AdminId = adminId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.CreditCards.AddAsync(card);
        await _unitOfWork.SaveChangesAsync();

        try
        {
            var body = $"Estimado(a) {client.FirstName}, se le ha asignado una tarjeta con límite de {dto.Limit}.";
            await _emailService.SendAsync(client.Email!, "Nueva Tarjeta de Crédito", body);
        }
        catch { }

        var resultDto = new CreditCardDto
        {
            Id = card.Id,
            MaskedCardNumber = MaskCardNumber(card.CardNumber),
            LastFourDigits = card.CardNumber[^4..],
            ClientId = card.ClientId,
            ClientFullName = $"{client.FirstName} {client.LastName}",
            CreditLimit = card.Limit,
            AvailableCredit = card.Limit,
            CurrentDebt = 0m,
            ExpirationDate = card.ExpirationDate,
            Status = card.Status.ToString(),
            CreatedAt = card.CreatedAt
        };

        return (true, null, resultDto);
    }

    public async Task<(bool Success, string? Error)> UpdateCreditCardLimitAsync(Guid id, UpdateCreditCardLimitDto dto)
    {
        var card = await _unitOfWork.CreditCards.GetByIdAsync(id);
        if (card == null) return (false, "Tarjeta no encontrada.");

        if (card.Status != CardStatus.Activa) return (false, "No se puede modificar el límite de una tarjeta cancelada.");
        if (dto.NewLimit <= 0) return (false, "El límite debe ser mayor a cero.");
        if (dto.NewLimit < card.Debt) return (false, "El límite no puede ser menor a la deuda actual.");

        card.Limit = dto.NewLimit;
        _unitOfWork.CreditCards.Update(card);
        await _unitOfWork.SaveChangesAsync();

        try
        {
            var client = await _userManager.FindByIdAsync(card.ClientId.ToString());
            if (client != null)
            {
                var body = $"Estimado(a) {client.FirstName}, el límite de su tarjeta ha sido actualizado a {dto.NewLimit}.";
                await _emailService.SendAsync(client.Email!, "Actualización de Límite", body);
            }
        }
        catch { }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CancelCreditCardAsync(Guid id)
    {
        var card = await _unitOfWork.CreditCards.GetByIdAsync(id);
        if (card == null) return (false, "Tarjeta no encontrada.");

        if (card.Status == CardStatus.Cancelada) return (false, "La tarjeta ya está cancelada.");
        if (card.Debt > 0) return (false, "No se puede cancelar una tarjeta con deuda.");

        card.Status = CardStatus.Cancelada;
        _unitOfWork.CreditCards.Update(card);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    private string MaskCardNumber(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4) return cardNumber;
        return "**** **** **** " + cardNumber.Substring(cardNumber.Length - 4);
    }

    public async Task<IEnumerable<CreditCardDto>> GetClientCardsAsync(Guid clientId)
    {
        var cards = await _unitOfWork.CreditCards.GetByClientIdAsync(clientId);
        var user = await _userManager.FindByIdAsync(clientId.ToString());

        return cards.Select(c => new CreditCardDto
        {
            Id = c.Id,
            MaskedCardNumber = MaskCardNumber(c.CardNumber),
            LastFourDigits = c.CardNumber.Length >= 4 ? c.CardNumber[^4..] : c.CardNumber,
            ClientId = c.ClientId,
            ClientFullName = user != null ? $"{user.FirstName} {user.LastName}" : "",
            CreditLimit = c.Limit,
            AvailableCredit = c.Limit - c.Debt,
            CurrentDebt = c.Debt,
            ExpirationDate = c.ExpirationDate,
            Status = c.Status.ToString(),
            CreatedAt = c.CreatedAt
        }).OrderByDescending(c => c.CreatedAt);
    }
}
