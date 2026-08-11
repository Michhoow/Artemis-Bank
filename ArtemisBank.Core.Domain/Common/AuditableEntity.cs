namespace ArtemisBank.Core.Domain.Common
{
    /// <summary>Base para entidades con traza de creacion. No se elimina fisicamente ningun registro financiero.</summary>
    public abstract class AuditableEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedByUserId { get; set; }
    }
}
