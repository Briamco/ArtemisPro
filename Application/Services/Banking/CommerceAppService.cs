using System;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Application.Services.Banking;

public class CommerceAppService : ICommerceAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public CommerceAppService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<PagedResultDto<CommerceDto>> GetCommercesPagedAsync(int page, int pageSize, string? status)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 20) pageSize = 20;

        var (items, totalRecords) = await _unitOfWork.Merchants.GetPagedAsync(page, pageSize, status);
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var dtoList = items.Select(m => new CommerceDto
        {
            Id = m.Id.ToString(),
            Name = m.Name,
            Description = m.Description,
            Email = m.Email,
            PhoneNumber = m.PhoneNumber,
            RNC = m.RNC,
            IsActive = m.Status == MerchantStatus.Activo,
            HasAssociatedUser = m.Users.Any(),
            CreatedAt = m.CreatedAt
        }).ToList();

        return new PagedResultDto<CommerceDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages > 0 ? totalPages : 1,
            Data = dtoList
        };
    }

    public async Task<CommerceDetailDto?> GetCommerceByIdAsync(Guid id)
    {
        var merchant = await _unitOfWork.Merchants.GetByIdWithUsersAsync(id);
        if (merchant == null) return null;

        var associatedUser = merchant.Users.FirstOrDefault();

        return new CommerceDetailDto
        {
            Id = merchant.Id.ToString(),
            Name = merchant.Name,
            Description = merchant.Description,
            Email = merchant.Email,
            PhoneNumber = merchant.PhoneNumber,
            RNC = merchant.RNC,
            IsActive = merchant.Status == MerchantStatus.Activo,
            CreatedAt = merchant.CreatedAt,
            AssociatedUser = associatedUser != null ? new CommerceAssociatedUserDto
            {
                Id = associatedUser.Id.ToString(),
                UserName = associatedUser.UserName ?? string.Empty,
                Email = associatedUser.Email ?? string.Empty,
                IsActive = associatedUser.IsActive
            } : null
        };
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, CommerceDto? Commerce)> CreateCommerceAsync(CreateCommerceDto dto, Guid? adminId)
    {
        var existingRnc = await _unitOfWork.Merchants.GetByRNCAsync(dto.RNC);
        if (existingRnc != null)
            return (false, "Conflict", "Ya existe un comercio con el mismo RNC o correo electrónico.", null);

        var existingEmail = await _unitOfWork.Merchants.GetByEmailAsync(dto.Email);
        if (existingEmail != null)
            return (false, "Conflict", "Ya existe un comercio con el mismo RNC o correo electrónico.", null);

        var merchant = new Merchant
        {
            Name = dto.Name,
            Description = dto.Description,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            RNC = dto.RNC,
            Status = MerchantStatus.Activo,
            AdminId = adminId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Merchants.AddAsync(merchant);
        await _unitOfWork.SaveChangesAsync();

        var responseDto = new CommerceDto
        {
            Id = merchant.Id.ToString(),
            Name = merchant.Name,
            Description = merchant.Description,
            Email = merchant.Email,
            PhoneNumber = merchant.PhoneNumber,
            RNC = merchant.RNC,
            IsActive = true,
            HasAssociatedUser = false,
            CreatedAt = merchant.CreatedAt
        };

        return (true, null, null, responseDto);
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> UpdateCommerceAsync(Guid id, UpdateCommerceDto dto)
    {
        var merchant = await _unitOfWork.Merchants.GetByIdAsync(id);
        if (merchant == null)
            return (false, "NotFound", "El comercio indicado no existe.");

        var existingRnc = await _unitOfWork.Merchants.GetByRNCAsync(dto.RNC);
        if (existingRnc != null && existingRnc.Id != id)
            return (false, "Conflict", "El RNC o correo electrónico pertenece a otro comercio.");

        var existingEmail = await _unitOfWork.Merchants.GetByEmailAsync(dto.Email);
        if (existingEmail != null && existingEmail.Id != id)
            return (false, "Conflict", "El RNC o correo electrónico pertenece a otro comercio.");

        merchant.Name = dto.Name;
        merchant.Description = dto.Description;
        merchant.Email = dto.Email;
        merchant.PhoneNumber = dto.PhoneNumber;
        merchant.RNC = dto.RNC;

        _unitOfWork.Merchants.Update(merchant);
        await _unitOfWork.SaveChangesAsync();

        return (true, null, null);
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> UpdateCommerceStatusAsync(Guid id, bool status)
    {
        var merchant = await _unitOfWork.Merchants.GetByIdWithUsersAsync(id);
        if (merchant == null)
            return (false, "NotFound", "El comercio indicado no existe.");

        merchant.Status = status ? MerchantStatus.Activo : MerchantStatus.Inactivo;
        _unitOfWork.Merchants.Update(merchant);

        if (!status)
        {
            // Al desactivar un comercio, todos los usuarios asociados a ese comercio deben quedar inactivos.
            foreach (var user in merchant.Users)
            {
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }
        }
        // Si status es true (reactivación), los usuarios asociados NO se activan automáticamente.

        await _unitOfWork.SaveChangesAsync();
        return (true, null, null);
    }
}
