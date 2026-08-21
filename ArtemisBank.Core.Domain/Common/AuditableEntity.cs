namespace ArtemisBank.Core.Domain.Common
{
    public abstract class AuditableEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedByUserId { get; set; }
    }
}
