using ArtemisBank.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBank.Infrastructure.Persistence.EntityConfigurations
{
    public class ConsumptionEntityConfiguration : IEntityTypeConfiguration<Consumption>
    {
        public void Configure(EntityTypeBuilder<Consumption> builder)
        {
            #region Configuracion basica
            builder.ToTable("Consumptions");
            builder.HasKey(k => k.Id);
            #endregion

            #region Propiedades
            builder.Property(k => k.Amount).HasPrecision(18, 2).IsRequired();

            builder.Property(k => k.CommerceName).IsRequired().HasMaxLength(150);
            builder.Property(k => k.CommerceId);

            builder.Property(k => k.Status).HasConversion<int>().IsRequired();
            builder.Property(k => k.RejectionReason).HasMaxLength(200);

            builder.Property(k => k.CreatedAt).IsRequired();
            builder.Property(k => k.CreatedByUserId).HasMaxLength(450);

            builder.Ignore(k => k.IsAdvance);
            #endregion

            #region Indices de consulta
            // Detalle de tarjeta: consumos del mas reciente al mas antiguo.
            builder.HasIndex(k => new { k.CreditCardId, k.CreatedAt });
            // Transacciones de Hermes Pay por comercio.
            builder.HasIndex(k => new { k.CommerceId, k.CreatedAt });
            #endregion
        }
    }
}
