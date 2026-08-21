using ArtemisBank.Core.Domain.Common;

namespace ArtemisBank.Core.Domain.Entities
{
    public class Commerce : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Rnc { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string AccountNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
