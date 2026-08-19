using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Banking;

public class CommerceDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string RNC { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool HasAssociatedUser { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CommerceDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string RNC { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public CommerceAssociatedUserDto? AssociatedUser { get; set; }
}

public class CommerceAssociatedUserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateCommerceDto
{
    [Required(ErrorMessage = "El nombre del comercio es obligatorio.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico debe tener un formato válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El RNC es obligatorio.")]
    public string RNC { get; set; } = string.Empty;
}

public class UpdateCommerceDto
{
    [Required(ErrorMessage = "El nombre del comercio es obligatorio.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico debe tener un formato válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El RNC es obligatorio.")]
    public string RNC { get; set; } = string.Empty;
}

public class UpdateCommerceStatusDto
{
    [Required]
    public bool Status { get; set; }
}
