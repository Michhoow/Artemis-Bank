using Microsoft.AspNetCore.Identity;

namespace ArtemisBank.Infrastructure.Identity.Entities
{
    /// <summary>
    /// Usuario de la aplicación. Extiende <see cref="IdentityUser"/> con los campos de negocio
    /// requeridos por la rúbrica: nombre, apellido, cédula y estado activo.
    /// Regla: los usuarios nuevos nacen con <see cref="IsActive"/> = false;
    /// solo el seed inicial y el administrador pueden activarlos.
    /// </summary>
    public class AppUser : IdentityUser
    {
        /// <summary>Nombre(s) del usuario.</summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>Apellido(s) del usuario.</summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Cédula (WebApp) o RNC (comercio). Único en el sistema.
        /// Se almacena como texto para preservar ceros iniciales.
        /// </summary>
        public string Identification { get; set; } = string.Empty;

        /// <summary>
        /// Indica si la cuenta está activa. Los usuarios nuevos nacen en false.
        /// Solo el seed y el administrador pueden activar cuentas.
        /// </summary>
        public bool IsActive { get; set; } = false;

        /// <summary>Nombre completo calculado (solo lectura).</summary>
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
