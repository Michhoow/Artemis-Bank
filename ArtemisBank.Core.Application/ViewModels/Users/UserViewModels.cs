using System.ComponentModel.DataAnnotations;
using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Common;

namespace ArtemisBank.Core.Application.ViewModels.Users
{
    public class UserIndexViewModel
    {
        public PagedResult<UserInfoDto> Users { get; set; } = PagedResult<UserInfoDto>.Empty();

        public string? Role { get; set; }

        public string? InfoMessage { get; set; }

        public static readonly string[] AvailableRoles =
            { "Administrador", "Cajero", "Cliente", "Comercio" };
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "El nombre es requerido.")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es requerido.")]
        [StringLength(100)]
        [Display(Name = "Apellido")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cédula es requerida.")]
        [RegularExpression(@"^\d{3}-?\d{7}-?\d{1}$",
            ErrorMessage = "La cédula debe tener el formato 000-0000000-0.")]
        [Display(Name = "Cédula")]
        public string Identification { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es requerido.")]
        [EmailAddress(ErrorMessage = "El correo indicado no tiene un formato válido.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de usuario es requerido.")]
        [MinLength(4, ErrorMessage = "El nombre de usuario debe tener al menos 4 caracteres.")]
        [Display(Name = "Nombre de usuario")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es requerida.")]
        [Compare(nameof(Password), ErrorMessage = AppMessages.UserPasswordsDoNotMatch)]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es requerido.")]
        [Display(Name = "Rol")]
        public string Role { get; set; } = "Cliente";

        [Range(0, 999999999.99, ErrorMessage = AppMessages.NegativeInitialBalance)]
        [Display(Name = "Monto inicial")]
        public decimal? InitialAmount { get; set; }

        public static readonly string[] AssignableRoles = { "Administrador", "Cajero", "Cliente" };
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es requerido.")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es requerido.")]
        [StringLength(100)]
        [Display(Name = "Apellido")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cédula es requerida.")]
        [RegularExpression(@"^\d{3}-?\d{7}-?\d{1}$",
            ErrorMessage = "La cédula debe tener el formato 000-0000000-0.")]
        [Display(Name = "Cédula")]
        public string Identification { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es requerido.")]
        [EmailAddress(ErrorMessage = "El correo indicado no tiene un formato válido.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de usuario es requerido.")]
        [MinLength(4, ErrorMessage = "El nombre de usuario debe tener al menos 4 caracteres.")]
        [Display(Name = "Nombre de usuario")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Rol")]
        public string Role { get; set; } = string.Empty;

        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña (opcional)")]
        public string? Password { get; set; }

        [Compare(nameof(Password), ErrorMessage = AppMessages.UserPasswordsDoNotMatch)]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nueva contraseña")]
        public string? ConfirmPassword { get; set; }

        [Range(0, 999999999.99, ErrorMessage = "El monto adicional no puede ser negativo.")]
        [Display(Name = "Monto adicional")]
        public decimal? AdditionalAmount { get; set; }

        public bool IsClient => string.Equals(Role, "Cliente", StringComparison.OrdinalIgnoreCase);
    }

    public class ChangeUserStatusViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public bool TargetStatus => !IsActive;

        public string ActionText => IsActive ? "Inactivar" : "Activar";

        public string ConfirmationMessage =>
            $"¿Está seguro que desea {ActionText.ToLowerInvariant()} al usuario {UserName}?";
    }
}
