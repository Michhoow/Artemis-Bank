using ArtemisBank.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBank.Infrastructure.Persistence.EntityConfigurations
{
    public class CreditCardEntityConfiguration : IEntityTypeConfiguration<CreditCard>
    {
        public void Configure(EntityTypeBuilder<CreditCard> builder)
        {
            #region Configuracion basica
            builder.ToTable("CreditCards");
            builder.HasKey(c => c.Id);
            #endregion

            #region Propiedades
            // Numero de 16 digitos como TEXTO. Nunca se expone completo fuera del modulo.
            builder.Property(c => c.CardNumber)
                .IsRequired()
                .HasMaxLength(16)
                .IsUnicode(false)
                .IsFixedLength();

            builder.HasIndex(c => c.CardNumber).IsUnique();

            builder.Property(c => c.ClientId).IsRequired().HasMaxLength(450);
            builder.HasIndex(c => c.ClientId);

            builder.Property(c => c.CreditLimit).HasPrecision(18, 2).IsRequired();
            builder.Property(c => c.Debt).HasPrecision(18, 2).IsRequired();

            // Hash SHA-256 en hexadecimal: 64 caracteres. Nunca el CVC en claro.
            builder.Property(c => c.CvcHash).IsRequired().HasMaxLength(128).IsUnicode(false);

            // MM/AA
            builder.Property(c => c.ExpirationDate).IsRequired().HasMaxLength(5).IsUnicode(false);

            builder.Property(c => c.Status).HasConversion<int>().IsRequired();

            builder.Property(c => c.CreatedAt).IsRequired();
            builder.Property(c => c.CreatedByUserId).HasMaxLength(450);
            builder.Property(c => c.AssignedByUserId).HasMaxLength(450);

            // Propiedades calculadas: no se mapean.
            builder.Ignore(c => c.IsActive);
            builder.Ignore(c => c.HasDebt);
            builder.Ignore(c => c.AvailableCredit);
            builder.Ignore(c => c.LastFourDigits);
            #endregion

            #region Relaciones
            builder.HasMany(c => c.Consumptions)
                .WithOne(k => k.CreditCard)
                .HasForeignKey(k => k.CreditCardId)
                // El historial de consumos nunca se elimina, ni al cancelar la tarjeta.
                .OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region Indices de consulta
            builder.HasIndex(c => new { c.ClientId, c.Status });
            builder.HasIndex(c => c.CreatedAt);
            #endregion
        }
    }
}
