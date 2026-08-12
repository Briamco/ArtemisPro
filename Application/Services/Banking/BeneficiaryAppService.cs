using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Banking;

public class BeneficiaryAppService : IBeneficiaryAppService
{
    private readonly IBeneficiaryRepository _beneficiaryRepository;
    private readonly ISavingsAccountRepository _savingsAccountRepository;
    private readonly IMapper _mapper;

    public BeneficiaryAppService(
        IBeneficiaryRepository beneficiaryRepository,
        ISavingsAccountRepository savingsAccountRepository,
        IMapper mapper)
    {
        _beneficiaryRepository = beneficiaryRepository;
        _savingsAccountRepository = savingsAccountRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<BeneficiaryDto>> GetByClientIdAsync(Guid clientId)
    {
        var beneficiaries = await _beneficiaryRepository.GetByClientIdAsync(clientId);
        var dtos = _mapper.Map<IEnumerable<BeneficiaryDto>>(beneficiaries).ToList();

        foreach (var dto in dtos)
        {
            var account = await _savingsAccountRepository.GetByAccountNumberAsync(dto.BeneficiaryAccountNumber);
            if (account != null && account.Client != null)
            {
                dto.OwnerFirstName = account.Client.FirstName;
                dto.OwnerLastName = account.Client.LastName;
            }
        }

        return dtos;
    }

    public async Task<(bool Success, string? Error)> CreateBeneficiaryAsync(Guid clientId, CreateBeneficiaryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.BeneficiaryAccountNumber) || dto.BeneficiaryAccountNumber.Length != 9 || !dto.BeneficiaryAccountNumber.All(char.IsDigit))
        {
            return (false, "La cuenta ingresada debe tener exactamente 9 dígitos numéricos.");
        }

        var account = await _savingsAccountRepository.GetByAccountNumberAsync(dto.BeneficiaryAccountNumber);
        if (account == null)
        {
            return (false, "El número de cuenta ingresado no corresponde a una cuenta válida.");
        }

        if (account.Status != AccountStatus.Activa)
        {
            return (false, "No puede agregar una cuenta cancelada como beneficiario.");
        }

        if (account.ClientId == clientId)
        {
            return (false, "No puede agregar una cuenta propia como beneficiario. Utilice la opción Transferencia para mover fondos entre sus cuentas.");
        }

        var existing = await _beneficiaryRepository.GetByClientAndAccountAsync(clientId, dto.BeneficiaryAccountNumber);
        if (existing != null)
        {
            return (false, "Esta cuenta ya se encuentra registrada como beneficiario.");
        }

        var beneficiary = new Beneficiary
        {
            ClientId = clientId,
            BeneficiaryAccountNumber = dto.BeneficiaryAccountNumber,
            Alias = dto.Alias,
            Status = BeneficiaryStatus.Activo,
            CreatedAt = DateTime.UtcNow
        };

        await _beneficiaryRepository.AddAsync(beneficiary);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteBeneficiaryAsync(Guid id)
    {
        var beneficiary = await _beneficiaryRepository.GetByIdAsync(id);
        if (beneficiary == null)
        {
            return (false, "Beneficiario no encontrado.");
        }

        await _beneficiaryRepository.DeleteAsync(beneficiary);
        return (true, null);
    }
}
