using ArtemisBank.Core.Domain.Common;

namespace ArtemisBank.Core.Domain.Entities
{
    /// <summary>
    /// Entidad Comercio para la Web API y Hermes Pay.
    /// Propiedad: Monserrat.
    /// Un comercio está vinculado a un usuario con rol Comercio y posee una cuenta de ahorro principal.
    /// </summary>
    public class Commerce : AuditableEntity
    {
        /// <summary>Nombre o Razón Social del comercio.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Registro Nacional de Contribuyente (RNC). Único.</summary>
        public string Rnc { get; set; } = string.Empty;

        /// <summary>Correo electrónico de contacto del comercio. Único.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Teléfono de contacto.</summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>ID del usuario (ASP.NET Identity) con rol Comercio asociado.</summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>Número de cuenta de ahorro principal vinculada al comercio (9 dígitos).</summary>
        public string AccountNumber { get; set; } = string.Empty;

        /// <summary>Indica si el comercio está activo para procesar pagos.</summary>
        public bool IsActive { get; set; } = true;
    }
}
