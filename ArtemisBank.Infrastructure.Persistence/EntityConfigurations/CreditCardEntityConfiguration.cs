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

            builder.Property(c => c.ExpirationMonth).IsRequired();
            builder.Property(c => c.ExpirationYear).IsRequired();

            builder.Property(c => c.CvcHash).IsRequired().HasMaxLength(88).IsUnicode(false);
            builder.Property(c => c.CvcSalt).IsRequired().HasMaxLength(44).IsUnicode(false);

            builder.Property(c => c.Status).HasConversion<int>().IsRequired();
            builder.Property(c => c.CreatedAt).IsRequired();
            builder.Property(c => c.CreatedByUserId).HasMaxLength(450);
            builder.Property(c => c.CancelledByUserId).HasMaxLength(450);

            builder.Ignore(c => c.IsActive);
            builder.Ignore(c => c.AvailableCredit);
            builder.Ignore(c => c.HasDebt);
            builder.Ignore(c => c.LastFourDigits);
            builder.Ignore(c => c.ExpirationDisplay);
            #endregion

            #region Relaciones
            builder.HasMany(c => c.Consumptions)
                .WithOne(k => k.CreditCard)
                .HasForeignKey(k => k.CreditCardId)

                .OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region Indices de consulta
            builder.HasIndex(c => new { c.ClientId, c.Status });
            builder.HasIndex(c => c.CreatedAt);
            #endregion
        }
    }
}
