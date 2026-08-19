using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Identity;

public class UserApiDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Identification { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CommerceUserApiDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Identification { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Comercio";
    public string CommerceId { get; set; } = string.Empty;
    public string CommerceName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateUserApiDto
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cédula es requerida.")]
    public string Identification { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es requerido.")]
    [EmailAddress(ErrorMessage = "El formato de correo no es válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es requerido.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de contraseña es requerida.")]
    [Compare("Password", ErrorMessage = "La contraseña y la confirmación deben coincidir.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es requerido.")]
    public string Role { get; set; } = string.Empty;

    public decimal? InitialAmount { get; set; }
}

public class CreateCommerceUserApiDto
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cédula o identificador es requerida.")]
    public string Identification { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es requerido.")]
    [EmailAddress(ErrorMessage = "El formato de correo no es válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es requerido.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de contraseña es requerida.")]
    [Compare("Password", ErrorMessage = "La contraseña y la confirmación deben coincidir.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "El balance inicial es requerido.")]
    public decimal InitialAmount { get; set; }
}

public class UpdateUserApiDto
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cédula es requerida.")]
    public string Identification { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es requerido.")]
    [EmailAddress(ErrorMessage = "El formato de correo no es válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es requerido.")]
    public string UserName { get; set; } = string.Empty;

    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public decimal? AdditionalAmount { get; set; }
}

public class UpdateUserStatusApiDto
{
    [Required]
    public bool Status { get; set; }
}

public class UserDetailApiDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Identification { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserMainAccountApiDto? MainAccount { get; set; }
}

public class UserMainAccountApiDto
{
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public bool IsPrincipal { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreatedUserResponseApiDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? CommerceId { get; set; }
    public bool IsActive { get; set; }
}
